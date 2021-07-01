using Markov;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;

namespace TwitchEbooks.Infrastructure
{
    public interface IMarkovChainService
    {
        Task AddChainsForChannelsAsync(IEnumerable<uint> channelIds, CancellationToken token = default);
        Task AddChainForChannelAsync(uint channelId, CancellationToken token = default);
        Task RemoveChainForChannelAsync(uint channelId, CancellationToken token = default);
        Task AddMessageAsync(uint channelId, string message, CancellationToken token = default);
        Task<string> GenerateMessageAsync(uint channelId, CancellationToken token = default);
    }

    public class MarkovChainService : IMarkovChainService
    {
        private readonly ILogger<MarkovChainService> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly Dictionary<uint, MarkovChain<string>> _channelChains;
        private readonly AsyncReaderWriterLock _readerWriterLock;

        public MarkovChainService(ILogger<MarkovChainService> logger, IDbContextFactory<TwitchEbooksContext> contextFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _channelChains = new Dictionary<uint, MarkovChain<string>>();
            _readerWriterLock = new AsyncReaderWriterLock();
        }

        public async Task AddChainsForChannelsAsync(IEnumerable<uint> channelIds, CancellationToken token = default)
        {
            await _readerWriterLock.AcquireWriterLock(token);
            try
            {
                await Task.WhenAll(channelIds.Select(id => AddOrUpdateChainForChannelAsync(id)));
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        public async Task AddChainForChannelAsync(uint channelId, CancellationToken token = default)
        {
            await _readerWriterLock.AcquireWriterLock(token);
            try
            {
                await AddOrUpdateChainForChannelAsync(channelId);
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        public async Task RemoveChainForChannelAsync(uint channelId, CancellationToken token = default)
        {
            await _readerWriterLock.AcquireWriterLock(token);
            if (!_channelChains.ContainsKey(channelId))
                throw new Exception("Could not remove chain, as one for the given channel does not exist.");

            try
            {
                _channelChains.Remove(channelId, out var _);
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        public async Task AddMessageAsync(uint channelId, string message, CancellationToken token = default)
        {
            await _readerWriterLock.AcquireWriterLock(token);
            if (!_channelChains.ContainsKey(channelId))
                throw new Exception("Could not add message as chain for given channel does not exist.");

            try
            {
                _channelChains[channelId].Add(message.Split(' '));
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        public async Task<string> GenerateMessageAsync(uint channelId, CancellationToken token = default)
        {
            await _readerWriterLock.AcquireReaderLock(token);
            if (!_channelChains.ContainsKey(channelId))
                return null;

            try
            {
                return string.Join(' ', _channelChains[channelId].Chain());
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
        }

        private async Task AddOrUpdateChainForChannelAsync(uint channelId)
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
    }
}
