using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Notifications;

namespace TwitchEbooks.Handlers.Notifications
{
    public class DeleteMessageNotificationHandler : INotificationHandler<DeleteMessageNotification>
    {
        private readonly ILogger<DeleteMessageNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;

        public DeleteMessageNotificationHandler(
            ILogger<DeleteMessageNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
        }

        public async Task Handle(DeleteMessageNotification notification, CancellationToken cancellationToken)
        {
            var messageId = notification.MessageId;
            var context = _contextFactory.CreateDbContext();

            // remove message from database
            var message = await context.Messages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
            context.Messages.Remove(message);
            await context.SaveChangesAsync(cancellationToken);

            // also refresh that channel's Markov chain
            await _chainService.AddChainForChannelAsync(message.ChannelId, cancellationToken);
        }
    }
}
