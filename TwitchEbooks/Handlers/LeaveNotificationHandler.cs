using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models;
using TwitchEbooks.Models.Notifications;
using TwitchEbooks.Twitch.Api;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers
{
    public class LeaveNotificationHandler : INotificationHandler<LeaveNotification>
    {
        private readonly ILogger<LeaveNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly TwitchApi _twitchApi;
        private readonly TwitchClient _twitchClient;
        private readonly TwitchSettings _twitchSettings;

        public LeaveNotificationHandler(
            ILogger<LeaveNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService,
            TwitchApi twitchApi,
            TwitchClient twitchClient,
            TwitchSettings twitchSettings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
            _twitchApi = twitchApi;
            _twitchClient = twitchClient;
            _twitchSettings = twitchSettings;
        }

        public async Task Handle(LeaveNotification notification, CancellationToken cancellationToken)
        {
            var context = _contextFactory.CreateDbContext();
            var channel = context.Channels.FirstOrDefault(c => c.Id == notification.ChannelId);
            if (channel is null)
                return;

            // disconnect from channel on Twitch
            // make sure a user exists on Twitch first
            var tokens = context.AccessTokens.OrderByDescending(t => t.CreatedOn).First();
            var usersResponse = await _twitchApi.GetUsersAsync(tokens.AccessToken, _twitchSettings.ClientId, ids: new List<string> { notification.ChannelId.ToString() });
            if (usersResponse.Users.Length == 1)
            {
                var user = usersResponse.Users[0];
                await _twitchClient.LeaveChannelAsync(user.Login);
            }

            // remove channel from database
            context.Channels.Add(new TwitchChannel
            {
                Id = notification.ChannelId
            });
            await context.SaveChangesAsync(cancellationToken);

            // also remove their Markov chain
            _chainService.RemoveChainForChannel(notification.ChannelId);
        }
    }
}
