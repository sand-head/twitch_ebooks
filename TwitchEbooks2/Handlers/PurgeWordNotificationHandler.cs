using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks2.Infrastructure;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
{
    public class PurgeWordNotificationHandler : INotificationHandler<PurgeWordNotification>
    {
        private readonly ILogger<PurgeWordNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;

        public PurgeWordNotificationHandler(
            ILogger<PurgeWordNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
        }

        public async Task Handle(PurgeWordNotification notification, CancellationToken cancellationToken)
        {
            var (channelId, word) = notification;
            var context = _contextFactory.CreateDbContext();
            var badMessages = context.Messages.Where(m => m.ChannelId == channelId && m.Message.ToLower().Contains(word.ToLower()));

            // ditch the bad messages
            context.Messages.RemoveRange(badMessages);
            var count = await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Purged {Count} messages from channel {Id}.", count, channelId);

            // re-create the pool for the given channel
            await _chainService.AddOrUpdateChainForChannel(channelId);
        }
    }
}
