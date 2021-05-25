using Anybotty.StreamClientLibrary.Common.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.Events;

namespace TwitchEbooks
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TwitchService _twitchService;
        private readonly MessageGenerationService _msgGenService;

        public Worker(ILogger<Worker> logger, TwitchService twitchService, MessageGenerationService msgGenService, IServiceProvider services)
        {
            _logger = logger;
            _twitchService = twitchService;
            _msgGenService = msgGenService;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _msgGenService.StartAsync(stoppingToken);

            var tokens = GetLatestTokens();
            if (tokens == null)
            {
                _logger.LogInformation("No access tokens were found in the database, pulling up the web server...");

                var webServerFactory = Services.GetService<AuthCodeFlowWebServerFactory>();
                using (var server = webServerFactory.CreateServer())
                {
                    var scopes = new string[]
                    {
                        "chat:edit",
                        "chat:read"
                    };

                    _logger.LogInformation("In order to connect to Twitch, we gotta do a fancy authentication handshake with them so that they'll give us some neat access tokens. To log in with Twitch, head on over to this URL:\n" + _twitchService.BuildAuthCodeFlowUrl(scopes));
                    tokens = await server.RunUntilCompleteAsync(stoppingToken); // blocks until authentication is complete
                    _logger.LogInformation("Alrighty, tokens acquired! We'll go ahead and continue starting up now, thanks.");
                }

                SaveTokens(tokens);
            }

            _twitchService.OnTokensRefreshed += (s, e) => SaveTokens(e.NewTokens);
            _twitchService.OnConnected += async (s, e) => await TwitchService_OnConnected();
            _twitchService.OnBotJoinLeaveReceivedEventArgs += (s, e) => TwitchService_OnBotJoinLeaveReceivedEventArgs(e);
            _twitchService.OnChatMessageReceived += _msgGenService.LoadChatMessage;
            _twitchService.OnGenerationRequestReceived += async (s, e) => await TwitchService_OnGenerationRequestReceived(e);
            _twitchService.OnChatMessageDeleted += TwitchService_OnChatMessageDeleted;
            _twitchService.OnGiftSubReceived += async (s, e) => await TwitchService_OnGiftSubReceived(e);
            _twitchService.OnPurgeWordRequestReceived += async (s, e) => await TwitchService_OnPurgeWordRequestReceived(e);
            _twitchService.OnBanUserRequestReceived += async (s, e) => await TwitchService_OnBanUserRequestReceived(e);
            await _twitchService.ConnectAsync(tokens);
        }

        private UserAccessToken GetLatestTokens()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            return context.AccessTokens.OrderByDescending(a => a.CreatedOn).FirstOrDefault();
        }

        private void SaveTokens(UserAccessToken tokens)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            context.AccessTokens.Add(tokens);
            context.SaveChanges();
            _logger.LogInformation("New access tokens saved.");
        }

        private async Task TwitchService_OnConnected()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();

            foreach (var channel in context.Channels)
            {
                await _twitchService.JoinChannelByIdAsync(channel.Id);
            }
        }

        private void TwitchService_OnBotJoinLeaveReceivedEventArgs(BotJoinLeaveReceivedEventArgs e)
        {
            if (e.RequestedPresence == BotPresenceRequest.Join)
                _msgGenService.TryAddPool(e.ChannelId);
            else if (e.RequestedPresence == BotPresenceRequest.Leave)
                _msgGenService.TryRemovePool(e.ChannelId);
        }

        private async Task TwitchService_OnGenerationRequestReceived(GenerationRequestReceivedEventArgs e)
        {
            var message = _msgGenService.GenerateMessage(e.ChannelId);
            await _twitchService.SendMessageAsync(e.ChannelName, message ?? "You gotta say stuff in chat before I can generate a message!");
        }

        private void TwitchService_OnChatMessageDeleted(object sender, MessageReceivedEventArgs<ClearMessage> e)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var message = context.Messages.Find(e.Message.MessageId);

            if (message != null)
            {
                // todo: rework this to use the solution for purging words
                context.Remove(message);
                context.SaveChanges();
            }
        }

        private async Task TwitchService_OnGiftSubReceived(MessageReceivedEventArgs<GiftSubscriptionMessage> e)
        {
            var message = _msgGenService.GenerateMessage(e.Message.ChannelId);
            // todo: this is gonna look real awkward if there aren't any messages stored, maybe address that at some point
            await _twitchService.SendMessageAsync(e.Message.ChannelName, $"🎉 Thanks @{e.Message.SenderName}! {message} 🎉");
        }

        private async Task TwitchService_OnPurgeWordRequestReceived(PurgeWordRequestReceivedEventArgs e)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var badMessages = context.Messages.Where(m => m.ChannelId == e.ChannelId && m.Message.Contains(e.Word));

            // ditch the bad messages
            context.Messages.RemoveRange(badMessages);
            context.SaveChanges();

            // re-create the pool for the given channel
            var messages = context.Messages.Where(m => m.ChannelId == e.ChannelId).AsAsyncEnumerable();
            await _msgGenService.LoadMessagesIntoPool(e.ChannelId, messages);
        }

        private async Task TwitchService_OnBanUserRequestReceived(BanUserRequestReceivedEventArgs e)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var badMessages = context.Messages.Where(m => m.ChannelId == e.ChannelId && m.UserId == e.UserId);

            // ditch the bad messages and add the user
            context.Messages.RemoveRange(badMessages);
            context.BannedTwitchUsers.Add(new BannedTwitchUser
            {
                Id = e.UserId,
                ChannelId = e.ChannelId,
                BannedOn = DateTime.UtcNow
            });
            context.SaveChanges();
            await _twitchService.SendMessageAsync(e.ChannelName, $"Alrighty, I won't listen to them anymore! Gimme a moment to reticulate my splines...");

            // re-create the pool for the given channel
            var messages = context.Messages.Where(m => m.ChannelId == e.ChannelId).AsAsyncEnumerable();
            await _msgGenService.LoadMessagesIntoPool(e.ChannelId, messages);
            await _twitchService.SendMessageAsync(e.ChannelName, $"Ok, I'm all set and ready to go!");
        }
    }
}
