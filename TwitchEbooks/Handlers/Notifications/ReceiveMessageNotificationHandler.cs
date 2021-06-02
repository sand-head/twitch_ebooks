using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Notifications;

namespace TwitchEbooks.Handlers.Notifications
{
    public class ReceiveMessageNotificationHandler : INotificationHandler<ReceiveMessageNotification>
    {
        private readonly ILogger<ReceiveMessageNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;

        public ReceiveMessageNotificationHandler(
            ILogger<ReceiveMessageNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
        }

        public async Task Handle(ReceiveMessageNotification notification, CancellationToken cancellationToken)
        {
            var message = notification.Message;

            var context = _contextFactory.CreateDbContext();
            // obviously, don't store messages by ignored users
            if (context.IgnoredUsers.Any(i => i.Id == message.UserId && i.ChannelId == message.RoomId)) return;

            var twitchMsg = new TwitchMessage
            {
                Id = message.Id,
                ChannelId = message.RoomId,
                UserId = message.UserId,
                Message = message.Message,
                ReceivedOn = DateTime.UtcNow
            };
            context.Messages.Add(twitchMsg);
            await context.SaveChangesAsync(cancellationToken);

            _chainService.AddMessage(twitchMsg.ChannelId, twitchMsg.Message);
        }
    }
}
