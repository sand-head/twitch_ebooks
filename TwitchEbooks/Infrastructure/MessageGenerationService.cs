using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchEbooks.Infrastructure
{
    public class MessageGenerationService
    {
        private readonly Dictionary<uint, MessageGenerationPool> _channelPools;

        public MessageGenerationService()
        {
            _channelPools = new Dictionary<uint, MessageGenerationPool>();
        }

        public bool TryAddPoolForChannel(uint channelId)
        {
            return _channelPools.TryAdd(channelId, new MessageGenerationPool());
        }
    }
}
