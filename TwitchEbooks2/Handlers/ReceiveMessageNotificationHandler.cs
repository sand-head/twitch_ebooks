using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks2.Infrastructure;
using TwitchEbooks2.Models.Notifications;

namespace TwitchEbooks2.Handlers
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

            var twitchMsg = new TwitchMessage
            {
                Id = Guid.Parse(message.Id),
                ChannelId = uint.Parse(message.RoomId),
                UserId = uint.Parse(message.UserId),
                Message = message.Message,
                ReceivedOn = DateTime.UtcNow
            };
            context.Messages.Add(twitchMsg);
            await context.SaveChangesAsync(cancellationToken);

            _chainService.AddMessage(twitchMsg.ChannelId, twitchMsg.Message);
        }
    }
}
