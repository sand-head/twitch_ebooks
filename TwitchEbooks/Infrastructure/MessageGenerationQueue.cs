using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TwitchEbooks.Infrastructure
{
    public class MessageGenerationQueue
    {
        private readonly Channel<uint> _channelRequestQueue;

        public MessageGenerationQueue()
        {
            _channelRequestQueue = Channel.CreateUnbounded<uint>();
        }

        public async ValueTask EnqueueAsync(uint channelId, CancellationToken cancellationToken = default)
        {
            await _channelRequestQueue.Writer.WriteAsync(channelId, cancellationToken);
        }

        public async ValueTask<uint> DequeueAsync(CancellationToken cancellationToken = default)
        {
            return await _channelRequestQueue.Reader.ReadAsync(cancellationToken);
        }
    }
}
