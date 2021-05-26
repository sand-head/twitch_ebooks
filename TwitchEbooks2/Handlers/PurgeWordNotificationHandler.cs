using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
{
    public class PurgeWordNotificationHandler : INotificationHandler<PurgeWordNotification>
    {
        private readonly ILogger<PurgeWordNotificationHandler> _logger;

        public PurgeWordNotificationHandler(ILogger<PurgeWordNotificationHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(PurgeWordNotification notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
