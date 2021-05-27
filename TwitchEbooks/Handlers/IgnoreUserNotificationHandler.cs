using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.Notifications;

namespace TwitchEbooks.Handlers
{
    public class IgnoreUserNotificationHandler : INotificationHandler<IgnoreUserNotification>
    {
        private readonly ILogger<IgnoreUserNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMediator _mediator;
        private readonly IMarkovChainService _chainService;

        public IgnoreUserNotificationHandler(
            ILogger<IgnoreUserNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMediator mediator,
            IMarkovChainService chainService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _mediator = mediator;
            _chainService = chainService;
        }

        public async Task Handle(IgnoreUserNotification notification, CancellationToken cancellationToken)
        {
            var (channelId, userId) = notification;

            var context = _contextFactory.CreateDbContext();
            var badMessages = context.Messages.Where(m => m.ChannelId == channelId && m.UserId == userId);

            // ditch the bad messages and add the user
            context.Messages.RemoveRange(badMessages);
            context.IgnoredUsers.Add(new IgnoredTwitchUser
            {
                Id = userId,
                ChannelId = channelId,
                BannedOn = DateTime.UtcNow
            });
            context.SaveChanges();
            await _mediator.Publish(new SendMessageNotification(channelId, "Alrighty, I won't listen to them anymore! Gimme a moment to reticulate my splines..."));

            // re-create the chain for the given channel
            var messages = context.Messages.Where(m => m.ChannelId == channelId).AsAsyncEnumerable();
            await _chainService.AddOrUpdateChainForChannel(channelId);
            await _mediator.Publish(new SendMessageNotification(channelId, "Ok, I'm all set and ready to go!"), cancellationToken);
        }
    }
}
