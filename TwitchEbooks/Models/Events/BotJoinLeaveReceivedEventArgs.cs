using System;

namespace TwitchEbooks.Models.Events
{
    public enum BotPresenceRequest
    {
        Join,
        Leave
    }

    public class BotJoinLeaveReceivedEventArgs : EventArgs
    {
        public BotPresenceRequest RequestedPresence { get; set; }
        public uint ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string BotChannelName { get; set; }
    }
}
