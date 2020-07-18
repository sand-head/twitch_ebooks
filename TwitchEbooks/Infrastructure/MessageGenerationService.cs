using Anybotty.StreamClientLibrary.Common.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            var channels = await context.Channels.Include(c => c.Messages).ToListAsync(token);

            foreach (var channel in channels)
            {
                _logger.LogInformation("Creating pool for channel {ChannelId}...", channel.Id);
                var stopwatch = Stopwatch.StartNew();
                var pool = new MessageGenerationPool();
                foreach (var message in channel.Messages)
                {
                    pool.LoadChatMessage(message);
                }
                _channelPools.Add(channel.Id, pool);
                stopwatch.Stop();
                _logger.LogInformation("Pool for channel {ChannelId} created. ({Elapsed} ms)", channel.Id, stopwatch.ElapsedMilliseconds);
            }
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
            if (!_channelPools.TryGetValue(channelId, out var pool)) throw new Exception("Could not generate a message for a pool that does not exist.");
            if (pool.LoadedMessages == 0) return null;
            return pool.GenerateMessage();
        }
    }
}
