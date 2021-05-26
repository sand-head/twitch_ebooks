using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
{
    public class JoinNotificationHandler : INotificationHandler<JoinNotification>
    {
        private readonly ILogger<JoinNotificationHandler> _logger;

        public JoinNotificationHandler(ILogger<JoinNotificationHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(JoinNotification notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
