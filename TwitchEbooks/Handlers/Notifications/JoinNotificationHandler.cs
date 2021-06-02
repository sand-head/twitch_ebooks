using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Notifications;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers.Notifications
{
    public class JoinNotificationHandler : INotificationHandler<JoinNotification>
    {
        private readonly ILogger<JoinNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly ITwitchUserService _userService;
        private readonly TwitchClient _twitchClient;

        public JoinNotificationHandler(
            ILogger<JoinNotificationHandler> logger,
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

        public async Task Handle(JoinNotification notification, CancellationToken cancellationToken)
        {
            var context = _contextFactory.CreateDbContext();
            if (context.Channels.Any(c => c.Id == notification.ChannelId))
                return;

            // make sure a user exists on Twitch first
            var channelName = await _userService.GetUsernameById(notification.ChannelId);

            // add channel to database
            context.Channels.Add(new TwitchChannel
            {
                Id = notification.ChannelId
            });
            await context.SaveChangesAsync(cancellationToken);

            // connect to channel on Twitch client
            await _twitchClient.JoinChannelAsync(channelName);
            // also create a Markov chain
            await _chainService.AddOrUpdateChainForChannel(notification.ChannelId);
        }
    }
}
