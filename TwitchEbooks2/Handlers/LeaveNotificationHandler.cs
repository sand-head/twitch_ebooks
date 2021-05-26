using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
{
    public class LeaveNotificationHandler : INotificationHandler<LeaveNotification>
    {
        private readonly ILogger<LeaveNotificationHandler> _logger;

        public LeaveNotificationHandler(ILogger<LeaveNotificationHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(LeaveNotification notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
