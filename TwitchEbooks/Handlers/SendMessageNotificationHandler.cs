using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Models;
using TwitchEbooks.Models.Notifications;
using TwitchEbooks.Twitch.Api;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers
{
    public class SendMessageNotificationHandler : INotificationHandler<SendMessageNotification>
    {
        private readonly ILogger<SendMessageNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly TwitchApi _api;
        private readonly TwitchClient _client;
        private readonly TwitchSettings _twitchSettings;

        public SendMessageNotificationHandler(
            ILogger<SendMessageNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            TwitchApi api,
            TwitchClient client,
            TwitchSettings twitchSettings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _api = api;
            _client = client;
            _twitchSettings = twitchSettings;
        }

        public async Task Handle(SendMessageNotification notification, CancellationToken cancellationToken)
        {
            var (channelId, message) = notification;
            var context = _contextFactory.CreateDbContext();
            var tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

            var response = await _api.GetUsersAsync(tokens.AccessToken, _twitchSettings.ClientId, ids: new List<string> { channelId.ToString() });
            if (response.Users.Length != 1)
                throw new Exception("Could not get user details by ID");
            var channelName = response.Users[0].Login;

            await _client.SendChatMessageAsync(channelName, message);
            _logger.LogInformation("Sent message to channel {Id}.", channelId);
        }
    }
}
