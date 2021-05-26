using Markov;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Infrastructure
{
    public class MessageGenerationService : BackgroundService
    {
        private readonly ILogger<MessageGenerationService> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMediator _mediator;
        private readonly MessageGenerationQueue _queue;
        // todo: move chains into their own singleton
        private readonly Dictionary<uint, MarkovChain<string>> _chains;

        public MessageGenerationService(ILogger<MessageGenerationService> logger, IDbContextFactory<TwitchEbooksContext> contextFactory, IMediator mediator, MessageGenerationQueue queue)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _mediator = mediator;
            _queue = queue;
            _chains = new Dictionary<uint, MarkovChain<string>>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeChains(stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var channelId = await _queue.DequeueAsync(stoppingToken);
                var message = GenerateMessage(channelId);
                await _mediator.Publish(new SendMessageNotification(channelId, message), stoppingToken);
            }
        }

        private async Task InitializeChains(CancellationToken stoppingToken)
        {
            var context = _contextFactory.CreateDbContext();
            var channels = await context.Channels.ToListAsync(stoppingToken);

            var tasks = channels.Select(c => CreateChainFromDatabase(c.Id));
            await Task.WhenAll(tasks);
        }

        private string GenerateMessage(uint channelId)
        {
            if (!_chains.ContainsKey(channelId))
                return null;
            return string.Join(' ', _chains[channelId].Chain());
        }

        private async Task CreateChainFromDatabase(uint channelId)
        {
            var context = _contextFactory.CreateDbContext();
            var messages = context.Messages.Where(m => m.ChannelId == channelId).AsAsyncEnumerable();

            _logger.LogInformation("Creating chain for channel {ChannelId}...", channelId);
            var stopwatch = Stopwatch.StartNew();
            var chain = new MarkovChain<string>(1);

            await foreach (var message in messages)
            {
                chain.Add(message.Message.Split(' '));
            }

            _chains.Add(channelId, chain);
            stopwatch.Stop();
            _logger.LogInformation("Chain for channel {ChannelId} created. ({Elapsed} ms)", channelId, stopwatch.ElapsedMilliseconds);
        }
    }
}
