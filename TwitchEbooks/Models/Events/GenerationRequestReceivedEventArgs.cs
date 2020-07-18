using System;

namespace TwitchEbooks.Models.Events
{
    public class GenerationRequestReceivedEventArgs : EventArgs
    {
        public uint ChannelId { get; set; }
        public string ChannelName { get; set; }
    }
}
