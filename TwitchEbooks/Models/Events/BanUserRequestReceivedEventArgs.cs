using System;

namespace TwitchEbooks.Models.Events
{
    public class BanUserRequestReceivedEventArgs : EventArgs
    {
        public uint ChannelId { get; set; }
        public uint UserId { get; set; }
    }
}
