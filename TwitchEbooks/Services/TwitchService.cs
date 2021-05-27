using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models;
using TwitchEbooks.Models.Notifications;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace TwitchEbooks.Services
{
    public class TwitchService : IHostedService
    {
        private readonly ILogger<TwitchService> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMediator _mediator;
        private readonly TwitchAPI _api;
        private readonly TwitchClient _client;
        private readonly TwitchSettings _settings;

        private UserAccessToken _tokens = null;
        private bool _isStopping = false;

        public TwitchService(
            ILogger<TwitchService> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IHttpClientFactory httpClientFactory,
            IMediator mediator,
            TwitchAPI api,
            TwitchClient client,
            TwitchSettings settings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _httpClientFactory = httpClientFactory;
            _mediator = mediator;
            _api = api;
            _client = client;
            _settings = settings;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var context = _contextFactory.CreateDbContext();
            // get and keep the latest auth tokens
            _tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

            // register event handlers
            _client.OnConnected += TwitchClient_OnConnected;
            _client.OnMessageReceived += TwitchClient_OnMessageReceived;
            _client.OnDisconnected += TwitchClient_OnDisconnected;

            // connect to Twitch
            _logger.LogInformation("Connecting to Twitch...");
            if (!_client.Connect())
            {
                throw new Exception("Could not connect client to Twitch");
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isStopping = true;
            _logger.LogInformation("Disconnecting from Twitch...");
            _client.Disconnect();
            return Task.CompletedTask;
        }

        private void TwitchClient_OnConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation("Connected!");
        }

        private async void TwitchClient_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var channelId = uint.Parse(e.ChatMessage.RoomId);
            var userId = uint.Parse(e.ChatMessage.UserId);

            if (channelId == _tokens.UserId)
            {
                // handle commands that are meant for the bot's chatroom
                if (e.ChatMessage.Message.StartsWith("~join"))
                    await _mediator.Publish(new JoinNotification(userId));
                else if (e.ChatMessage.Message.StartsWith("~leave"))
                    await _mediator.Publish(new LeaveNotification(userId));
            }
            else
            {
                // handle commands that are meant for a user's chatroom
                if (e.ChatMessage.Message.StartsWith("~generate"))
                    await _mediator.Publish(new GenerateMessageNotification(channelId));
                else if (e.ChatMessage.Message.StartsWith("~leave") && e.ChatMessage.IsBroadcaster)
                    await _mediator.Publish(new LeaveNotification(channelId));
                else if (e.ChatMessage.Message.StartsWith("~purge") && (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator))
                {
                    var splitMsg = e.ChatMessage.Message.Split(' ');
                    if (splitMsg.Length <= 1 || string.IsNullOrWhiteSpace(splitMsg[1]))
                    {
                        await _mediator.Publish(new SendMessageNotification(channelId, $"@{e.ChatMessage.Username} You have to include a word to purge!"));
                        return;
                    }

                    await _mediator.Publish(new PurgeWordNotification(channelId, splitMsg[1]));
                }
                else if (e.ChatMessage.Message.StartsWith("~ignore") && (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator))
                {
                    var splitMsg = e.ChatMessage.Message.Split(' ');
                    if (splitMsg.Length <= 1 || string.IsNullOrWhiteSpace(splitMsg[1]))
                    {
                        await _mediator.Publish(new SendMessageNotification(channelId, $"@{e.ChatMessage.Username} You have to include a username to ban!"));
                        return;
                    }

                    var userName = splitMsg[1].Trim();
                    var usersResponse = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { userName });
                    if (usersResponse.Users.Length != 1)
                    {
                        await _mediator.Publish(new SendMessageNotification(channelId, $"@{e.ChatMessage.Username} Couldn't find a user by the name of \"${userName}\", sorry!"));
                        return;
                    }

                    var ignoreUserId = uint.Parse(usersResponse.Users[0].Id);
                    await _mediator.Publish(new IgnoreUserNotification(channelId, ignoreUserId));
                }
                else
                    await _mediator.Publish(new ReceiveMessageNotification(e.ChatMessage));
            }
        }

        private async void TwitchClient_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            if (_isStopping)
            {
                _logger.LogInformation("Disconnected!");
                return;
            }

            _logger.LogInformation("Client disconnected unexpectedly, refreshing tokens...");
            var refreshResponse = await _api.V5.Auth.RefreshAuthTokenAsync(_tokens.RefreshToken, _settings.ClientSecret);
            var createdOn = DateTime.UtcNow;

            // manually validate tokens
            // I cannot believe TwitchLib is only *just now* adding this endpoint
            // for a library with so much community use how is it not actively supported
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth/validate");
            request.Headers.Add("Authorization", $"OAuth {refreshResponse.AccessToken}");
            var response = await httpClient.SendAsync(request);
            var validateResponse = await response.Content.ReadFromJsonAsync<ValidateResponse>();

            // set API access token and new credentials and reconnect to chat
            _api.Settings.AccessToken = refreshResponse.AccessToken;
            _client.SetConnectionCredentials(new ConnectionCredentials(validateResponse.Login, refreshResponse.AccessToken));
            _client.Connect();

            await _mediator.Publish(new RefreshedTokensNotification(new UserAccessToken
            {
                UserId = _tokens.UserId,
                AccessToken = refreshResponse.AccessToken,
                RefreshToken = refreshResponse.RefreshToken,
                ExpiresIn = refreshResponse.ExpiresIn,
                CreatedOn = createdOn
            }));
        }
    }
}
