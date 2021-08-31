using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TwitchEbooks.Models.MediatR.Notifications;

namespace TwitchEbooks.Infrastructure
{
    public class MessageGenerationQueue
    {
        private readonly Channel<GenerateMessageNotification> _channelRequestQueue;

        public MessageGenerationQueue()
        {
            _channelRequestQueue = Channel.CreateUnbounded<GenerateMessageNotification>();
        }

        public async ValueTask EnqueueAsync(GenerateMessageNotification notification, CancellationToken cancellationToken = default)
        {
            await _channelRequestQueue.Writer.WriteAsync(notification, cancellationToken);
        }

        public async ValueTask<GenerateMessageNotification> DequeueAsync(CancellationToken cancellationToken = default)
        {
            return await _channelRequestQueue.Reader.ReadAsync(cancellationToken);
        }
    }
}
