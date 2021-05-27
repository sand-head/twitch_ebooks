using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Models.Notifications;

namespace TwitchEbooks.Infrastructure
{
    public class MessageGenerationService : BackgroundService
    {
        private readonly ILogger<MessageGenerationService> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMediator _mediator;
        private readonly IMarkovChainService _chainService;
        private readonly MessageGenerationQueue _queue;

        public MessageGenerationService(
            ILogger<MessageGenerationService> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMediator mediator,
            IMarkovChainService chainService,
            MessageGenerationQueue queue)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _mediator = mediator;
            _chainService = chainService;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeChains(stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var channelId = await _queue.DequeueAsync(stoppingToken);
                var message = _chainService.GenerateMessage(channelId);
                _logger.LogInformation("Generated message for channel {Id}.", channelId);
                await _mediator.Publish(new SendMessageNotification(channelId, message), stoppingToken);
            }
        }

        private async Task InitializeChains(CancellationToken stoppingToken)
        {
            var context = _contextFactory.CreateDbContext();
            var channels = await context.Channels.ToListAsync(stoppingToken);

            var tasks = channels.Select(c => _chainService.AddOrUpdateChainForChannel(c.Id));
            await Task.WhenAll(tasks);
        }
    }
}
