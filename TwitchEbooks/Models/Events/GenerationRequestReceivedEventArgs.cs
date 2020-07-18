using System;

namespace TwitchEbooks.Models.Events
{
    public class GenerationRequestReceivedEventArgs : EventArgs
    {
        public string ChannelName { get; set; }
        public uint ChannelId { get; set; }
    }
}
