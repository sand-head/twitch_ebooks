﻿using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.Notifications;

namespace TwitchEbooks.Handlers
{
    public class GenerateMessageNotificationHandler : INotificationHandler<GenerateMessageNotification>
    {
        private readonly ILogger<GenerateMessageNotificationHandler> _logger;
        private readonly MessageGenerationQueue _queue;

        public GenerateMessageNotificationHandler(ILogger<GenerateMessageNotificationHandler> logger, MessageGenerationQueue queue)
        {
            _logger = logger;
            _queue = queue;
        }

        public async Task Handle(GenerateMessageNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding generation request for channel {Id} to queue.", notification.ChannelId);
            await _queue.EnqueueAsync(notification.ChannelId, cancellationToken);
        }
    }
}