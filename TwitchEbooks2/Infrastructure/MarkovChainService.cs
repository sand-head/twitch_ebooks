using Markov;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TwitchEbooks.Database;

namespace TwitchEbooks2.Infrastructure
{
    public interface IMarkovChainService
    {
        Task AddOrUpdateChainForChannel(uint channelId);
        void RemoveChainForChannel(uint channelId);
        void AddMessage(uint channelId, string message);
        string GenerateMessage(uint channelId);
    }

    public class MarkovChainService : IMarkovChainService
    {
        private readonly ILogger<MarkovChainService> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly ConcurrentDictionary<uint, MarkovChain<string>> _channelChains;

        public MarkovChainService(ILogger<MarkovChainService> logger, IDbContextFactory<TwitchEbooksContext> contextFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _channelChains = new ConcurrentDictionary<uint, MarkovChain<string>>();
        }

        public async Task AddOrUpdateChainForChannel(uint channelId)
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

            _channelChains[channelId] = chain;
            stopwatch.Stop();
            _logger.LogInformation("Chain for channel {ChannelId} created. ({Elapsed} ms)", channelId, stopwatch.ElapsedMilliseconds);
        }

        public void RemoveChainForChannel(uint channelId)
        {
            if (!_channelChains.ContainsKey(channelId))
                throw new Exception("Could not remove chain, as one for the given channel does not exist.");
            _channelChains.TryRemove(channelId, out var _);
        }

        public void AddMessage(uint channelId, string message)
        {
            if (!_channelChains.ContainsKey(channelId))
                throw new Exception("Could not add message as chain for given channel does not exist.");
            _channelChains[channelId].Add(message.Split(' '));
        }

        public string GenerateMessage(uint channelId)
        {
            if (!_channelChains.ContainsKey(channelId))
                return null;
            return string.Join(' ', _channelChains[channelId].Chain());
        }
    }
}
