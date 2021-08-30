using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models;
using TwitchEbooks.Models.MediatR.Notifications;
using TwitchEbooks.Models.MediatR.Requests;
using TwitchEbooks.Twitch.Api;
using TwitchEbooks.Twitch.Chat;
using TwitchEbooks.Twitch.Chat.EventArgs;
using TwitchEbooks.Twitch.Chat.Messages;

namespace TwitchEbooks.Services
{
    public class TwitchService : BackgroundService
    {
        private readonly ILogger<TwitchService> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMediator _mediator;
        private readonly ITwitchUserService _userService;
        private readonly TwitchApiFactory _apiFactory;
        private readonly TwitchClient _client;
        private readonly TwitchSettings _settings;

        private Database.Models.UserAccessToken _tokens = null;
        private bool _isStopping = false;

        public TwitchService(
            ILogger<TwitchService> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMediator mediator,
            ITwitchUserService userService,
            TwitchApiFactory apiFactory,
            TwitchClient client,
            TwitchSettings settings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _mediator = mediator;
            _userService = userService;
            _apiFactory = apiFactory;
            _client = client;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // get and keep the latest auth tokens
            using (var context = _contextFactory.CreateDbContext())
            {
                _tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();
            }

            // validate our tokens and refresh if necessary
            var api = _apiFactory.CreateApiClient();
            var response = await api.VerifyAccessTokenAsync(_tokens.AccessToken);
            if (response is null)
            {
                _tokens = await _mediator.Send(new RefreshTokensRequest(), stoppingToken);
            }

            // hook up events
            _client.OnDisconnected += TwitchClient_OnDisconnected;

            // connect to Twitch
            _logger.LogInformation("Connecting to Twitch...");
            await _client.ConnectAsync(_settings.BotUsername, _tokens.AccessToken, token: stoppingToken);

            // continue to read messages until the application stops
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await _client.ReadMessageAsync(stoppingToken);
                try
                {
                    switch (message)
                    {
                        case TwitchMessage.Welcome:
                            _logger.LogInformation("Connected!");
                            await TwitchClient_OnConnected();
                            break;
                        case TwitchMessage.Join join:
                            _logger.LogDebug("{User} has joined channel {Channel}.", join.Username, join.Channel);
                            break;
                        case TwitchMessage.Chat chat:
                            await TwitchClient_OnMessageReceived(chat);
                            break;
                        case TwitchMessage.ClearChat clearChat:
                            var channelId = await _userService.GetIdByUsername(clearChat.Channel);
                            if (string.IsNullOrWhiteSpace(clearChat.User))
                            {
                                // todo: clear the entire channel's history
                            }
                            else
                            {
                                await _mediator.Send(new BanUserRequest(clearChat.Channel, clearChat.User), stoppingToken);
                            }
                            break;
                        case TwitchMessage.ClearMsg clearMsg:
                            await _mediator.Send(new DeleteMessageNotification(clearMsg.TargetMessageId), stoppingToken);
                            break;
                        case TwitchMessage.GiftSub giftSub:
                            await TwitchClient_OnGiftedSubscription(giftSub);
                            break;
                        case TwitchMessage.Part part:
                            _logger.LogDebug("{User} has left channel {Channel}.", part.Username, part.Channel);
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Exception caught in message read loop after receiving {MessageType}: {Exception}", message.GetType(), e);
                }
            }

            _isStopping = true;
            _logger.LogInformation("Disconnecting from Twitch...");
            _client.Disconnect();
        }

        private async Task TwitchClient_OnConnected()
        {
            var context = _contextFactory.CreateDbContext();
            var api = _apiFactory.CreateApiClient();

            var usersResponse = await api.GetUsersAsync(_tokens.AccessToken, _settings.ClientId, ids: context.Channels.Select(c => c.Id.ToString()).ToList());
            foreach (var user in usersResponse.Users)
            {
                _logger.LogInformation("Joining channel {Name}...", user.Login);
                await _client.JoinChannelAsync(user.Login);
            }
        }

        private async Task TwitchClient_OnMessageReceived(TwitchMessage.Chat chat)
        {
            var channelId = chat.RoomId;
            var userId = chat.UserId;

            if (_tokens.UserId == channelId)
            {
                // handle commands that are meant for the bot's chatroom
                if (chat.Message.StartsWith("~join"))
                    await _mediator.Send(new JoinRequest(userId));
                else if (chat.Message.StartsWith("~leave"))
                    await _mediator.Send(new LeaveRequest(userId));
            }
            else
            {
                // handle commands that are meant for a user's chatroom
                if (chat.Message.StartsWith("~generate"))
                    await _mediator.Publish(new GenerateMessageNotification(channelId));
                else if (chat.Message.StartsWith("~leave") && chat.IsBroadcaster)
                    await _mediator.Send(new LeaveRequest(channelId));
                else if (chat.Message.StartsWith("~purge") && (chat.IsBroadcaster || chat.IsModerator))
                {
                    var splitMsg = chat.Message.Split(' ');
                    if (splitMsg.Length <= 1 || string.IsNullOrWhiteSpace(splitMsg[1]))
                    {
                        await _mediator.Send(new SendMessageRequest(channelId, $"@{chat.Username} You have to include a word to purge!"));
                        return;
                    }

                    await _mediator.Publish(new PurgeWordNotification(channelId, splitMsg[1]));
                }
                else if (chat.Message.StartsWith("~ignore") && (chat.IsBroadcaster || chat.IsModerator))
                {
                    var splitMsg = chat.Message.Split(' ');
                    if (splitMsg.Length <= 1 || string.IsNullOrWhiteSpace(splitMsg[1]))
                    {
                        await _mediator.Send(new SendMessageRequest(channelId, $"@{chat.Username} You have to include a username to ban!"));
                        return;
                    }

                    var userName = splitMsg[1].Trim();
                    var ignoreUserId = await _userService.GetIdByUsername(userName);
                    await _mediator.Publish(new IgnoreUserNotification(channelId, ignoreUserId));
                }
                else
                    await _mediator.Publish(new ReceiveMessageNotification(chat));
            }
        }

        private async Task TwitchClient_OnGiftedSubscription(TwitchMessage.GiftSub giftSub)
        {
            // if the gift sub is to a channel we're in (and it's for the bot), send the celebratory messages
            if (_tokens.UserId == giftSub.RecipientId && _client.JoinedChannels.Contains(giftSub.Channel))
            {
                _logger.LogInformation("We got gifted a subscription to channel {Id}!", giftSub.RoomId);
                await _mediator.Send(new SendMessageRequest(giftSub.RoomId, $"🎉 Thanks for the gift sub @{giftSub.SenderDisplayName}! 🎉"));
                await _mediator.Publish(new GenerateMessageNotification(giftSub.RoomId));
            }
        }

        private async void TwitchClient_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            if (_isStopping)
            {
                _logger.LogInformation("Disconnected!");
                return;
            }

            // wait until it's *really* disconnected
            while (_client.IsConnected) { }
            _logger.LogInformation("Client disconnected unexpectedly!");

            // refresh tokens and reconnect to chat
            _tokens = await _mediator.Send(new RefreshTokensRequest());
            await _client.ReconnectAsync(accessToken: _tokens.AccessToken);
        }
    }
}
