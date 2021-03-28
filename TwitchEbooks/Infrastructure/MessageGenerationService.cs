using Anybotty.StreamClientLibrary.Common.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models.Events;

namespace TwitchEbooks.Infrastructure
{
    public class MessageGenerationService
    {
        private readonly ILogger<MessageGenerationService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Dictionary<uint, MessageGenerationPool> _channelPools;

        public MessageGenerationService(ILogger<MessageGenerationService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _channelPools = new Dictionary<uint, MessageGenerationPool>();
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            async Task LoadMessagesFromDatabase(uint channelId)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();

                var messages = context.Messages.Where(m => m.ChannelId == channelId).AsAsyncEnumerable();
                await LoadMessagesIntoPool(channelId, messages);
            }

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var channels = await context.Channels.ToListAsync(token);

            var tasks = channels.Select(c => LoadMessagesFromDatabase(c.Id));
            await Task.WhenAll(tasks);
        }

        public async Task LoadMessagesIntoPool(uint channelId, IAsyncEnumerable<TwitchMessage> messages)
        {
            // if the pool already exists, remove it
            // at this point we want an entirely new pool
            if (_channelPools.ContainsKey(channelId))
                _channelPools.Remove(channelId);

            _logger.LogInformation("Creating pool for channel {ChannelId}...", channelId);
            var stopwatch = Stopwatch.StartNew();
            var pool = new MessageGenerationPool();

            await foreach (var message in messages)
            {
                pool.LoadChatMessage(message);
            }

            _channelPools.Add(channelId, pool);
            stopwatch.Stop();
            _logger.LogInformation("Pool for channel {ChannelId} created. ({Elapsed} ms)", channelId, stopwatch.ElapsedMilliseconds);
        }

        public bool TryAddPool(uint channelId)
        {
            if (!_channelPools.TryAdd(channelId, new MessageGenerationPool())) return false;
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            context.Channels.Add(new TwitchChannel
            {
                Id = channelId
            });
            context.SaveChanges();
            return true;
        }

        public bool TryRemovePool(uint channelId)
        {
            if (!_channelPools.Remove(channelId)) return false;
            using var scope = _scopeFactory.CreateScope();
            // holy shit did I really make this also delete the channel from the database???
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var channel = context.Channels.Find(channelId);
            context.Channels.Remove(channel);
            context.SaveChanges();
            return true;
        }

        public void LoadChatMessage(object sender, MessageReceivedEventArgs<ChatMessage> e)
        {
            // this is only meant to be used by the TwitchService
            if (!_channelPools.TryGetValue(e.Message.RoomId, out var pool)) return;

            var message = new TwitchMessage
            {
                Id = e.Message.MessageId,
                ChannelId = e.Message.RoomId,
                UserId = e.Message.UserId,
                Message = e.Message.Message,
                ReceivedOn = DateTime.UtcNow
            };
            pool.LoadChatMessage(message);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            context.Messages.Add(message);
            context.SaveChanges();
        }

        public string GenerateMessage(uint channelId)
        {
            if (!_channelPools.TryGetValue(channelId, out var pool))
                throw new Exception("Could not generate a message for a pool that does not exist.");
            if (pool.LoadedMessages == 0)
                return null;
            return pool.GenerateMessage();
        }
    }
}
