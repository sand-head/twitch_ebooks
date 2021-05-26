using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
{
    public class IgnoreUserNotificationHandler : INotificationHandler<IgnoreUserNotification>
    {
        private readonly ILogger<IgnoreUserNotificationHandler> _logger;

        public IgnoreUserNotificationHandler(ILogger<IgnoreUserNotificationHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(IgnoreUserNotification notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
