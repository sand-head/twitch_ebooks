using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Notifications;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers.Notifications
{
    public class LeaveNotificationHandler : INotificationHandler<LeaveNotification>
    {
        private readonly ILogger<LeaveNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly ITwitchUserService _userService;
        private readonly TwitchClient _twitchClient;

        public LeaveNotificationHandler(
            ILogger<LeaveNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService,
            ITwitchUserService userService,
            TwitchClient twitchClient)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
            _userService = userService;
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
            var channelName = await _userService.GetUsernameById(notification.ChannelId);

            // leave channel on Twitch client
            await _twitchClient.LeaveChannelAsync(channelName);

            // remove channel from database
            context.Channels.Remove(channel);
            await context.SaveChangesAsync(cancellationToken);

            // also remove their Markov chain
            _chainService.RemoveChainForChannel(notification.ChannelId);
        }
    }
}
