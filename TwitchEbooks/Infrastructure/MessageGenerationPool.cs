using Markov;
using TwitchEbooks.Database.Models;

namespace TwitchEbooks.Infrastructure
{
    public class MessageGenerationPool
    {
        private readonly MarkovChain<string> _chain;

        public MessageGenerationPool()
        {
            _chain = new MarkovChain<string>(1);
            LoadedMessages = 0;
        }

        public int LoadedMessages { get; private set; }

        public void LoadChatMessage(TwitchMessage message)
        {
            _chain.Add(message.Message.Split(' '));
            LoadedMessages++;
        }

        public string GenerateMessage()
        {
            return string.Join(' ', _chain.Chain());
        }
    }
}
