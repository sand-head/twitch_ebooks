using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks2.Infrastructure;
using TwitchEbooks2.Models.Notifications;
using TwitchLib.Api;
using TwitchLib.Client;

namespace TwitchEbooks2.Handlers
{
    public class LeaveNotificationHandler : INotificationHandler<LeaveNotification>
    {
        private readonly ILogger<LeaveNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly TwitchAPI _twitchApi;
        private readonly TwitchClient _twitchClient;

        public LeaveNotificationHandler(
            ILogger<LeaveNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService,
            TwitchAPI twitchApi,
            TwitchClient twitchClient)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
            _twitchApi = twitchApi;
            _twitchClient = twitchClient;
        }

        public async Task Handle(LeaveNotification notification, CancellationToken cancellationToken)
        {
            var context = _contextFactory.CreateDbContext();
            var channel = context.Channels.FirstOrDefault(c => c.Id == notification.ChannelId);
            if (channel is null)
                return;

            // disconnect from channel on Twitch
            // make sure a user exists on Twitch first
            var usersResponse = await _twitchApi.Helix.Users.GetUsersAsync(ids: new List<string> { notification.ChannelId.ToString() });
            if (usersResponse.Users.Length == 1)
            {
                var user = usersResponse.Users[0];
                _twitchClient.LeaveChannel(user.Login);
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
