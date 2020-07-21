﻿using Anybotty.StreamClientLibrary.Common.Models.Messages;
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

        public Worker(ILogger<Worker> logger, IServiceProvider services)
        {
            _logger = logger;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var twitchService = Services.GetService<TwitchService>();
            var msgGenService = Services.GetService<MessageGenerationService>();

            await msgGenService.StartAsync(stoppingToken);

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

                    _logger.LogInformation("In order to connect to Twitch, we gotta do a fancy authentication handshake with them so that they'll give us some neat access tokens. To log in with Twitch, head on over to this URL:\n" + twitchService.BuildAuthCodeFlowUrl(scopes));
                    tokens = await server.RunUntilCompleteAsync(stoppingToken); // blocks until authentication is complete
                    _logger.LogInformation("Alrighty, tokens acquired! We'll go ahead and continue starting up now, thanks.");
                }

                SaveTokens(tokens);
            }

            // passing the services like this feels wrong, maybe just make them private properties?
            // that doesn't strike me as great either though
            twitchService.OnTokensRefreshed += (s, e) => SaveTokens(e.NewTokens);
            twitchService.OnConnected += async (s, e) => await TwitchService_OnConnected(twitchService);
            twitchService.OnBotJoinLeaveReceivedEventArgs += (s, e) => TwitchService_OnBotJoinLeaveReceivedEventArgs(e, msgGenService);
            twitchService.OnChatMessageReceived += msgGenService.LoadChatMessage;
            twitchService.OnGenerationRequestReceived += async (s, e) => await TwitchService_OnGenerationRequestReceived(e, twitchService, msgGenService);
            twitchService.OnChatMessageDeleted += TwitchService_OnChatMessageDeleted;
            twitchService.OnGiftSubReceived += async (s, e) => await TwitchService_OnGiftSubReceived(twitchService, msgGenService, e);
            await twitchService.ConnectAsync(tokens);
            // todo: execute until stop? or do we already do that?
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

        private async Task TwitchService_OnConnected(TwitchService twitchService)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();

            foreach (var channel in context.Channels)
            {
                await twitchService.JoinChannelByIdAsync(channel.Id);
            }
        }

        private void TwitchService_OnBotJoinLeaveReceivedEventArgs(BotJoinLeaveReceivedEventArgs e, MessageGenerationService msgGenService)
        {
            if (e.RequestedPresence == BotPresenceRequest.Join)
                msgGenService.TryAddPool(e.ChannelId);
            else if (e.RequestedPresence == BotPresenceRequest.Leave)
                msgGenService.TryRemovePool(e.ChannelId);
        }

        private async Task TwitchService_OnGenerationRequestReceived(GenerationRequestReceivedEventArgs e, TwitchService twitchService, MessageGenerationService msgGenService)
        {
            var message = msgGenService.GenerateMessage(e.ChannelId);
            await twitchService.SendMessageAsync(e.ChannelName, message ?? "You gotta say stuff in chat before I can generate a message!");
        }

        private void TwitchService_OnChatMessageDeleted(object sender, MessageReceivedEventArgs<ClearMessage> e)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var message = context.Messages.Find(e.Message.MessageId);
            if (message != null)
            {
                // we don't actually have a way to remove a message in a Markov chain
                // so for now at least we're just gonna rely on them being purged on program restart
                context.Remove(message);
                context.SaveChanges();
            }
        }

        private async Task TwitchService_OnGiftSubReceived(TwitchService twitchService, MessageGenerationService msgGenService, MessageReceivedEventArgs<GiftSubscriptionMessage> e)
        {
            var message = msgGenService.GenerateMessage(e.Message.ChannelId);
            await twitchService.SendMessageAsync(e.Message.ChannelName, $"🎉 Thanks @{e.Message.SenderName}! {message} 🎉");
        }
    }
}
