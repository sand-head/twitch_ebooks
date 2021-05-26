using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
{
    public class ReceiveMessageNotificationHandler : INotificationHandler<ReceiveMessageNotification>
    {
        private readonly ILogger<ReceiveMessageNotificationHandler> _logger;

        public ReceiveMessageNotificationHandler(ILogger<ReceiveMessageNotificationHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ReceiveMessageNotification notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
