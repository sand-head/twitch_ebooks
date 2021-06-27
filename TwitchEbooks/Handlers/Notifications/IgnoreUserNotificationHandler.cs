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
using TwitchEbooks.Models.MediatR.Notifications;
using TwitchEbooks.Models.MediatR.Requests;

namespace TwitchEbooks.Handlers.Notifications
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
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Now ignoring user {UserId} for channel {ChannelId}.", userId, channelId);
            await _mediator.Send(new SendMessageRequest(channelId, "Alrighty, I won't listen to them anymore! Also, I'll forget all the things they've said up till now."), cancellationToken);

            // re-create the chain for the given channel
            await _chainService.AddChainForChannelAsync(channelId, cancellationToken);
        }
    }
}
