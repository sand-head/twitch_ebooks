using System;

namespace TwitchEbooks.Database.Models
{
    public class TwitchMessage
    {
        public Guid Id { get; set; }
        public uint ChannelId { get; set; }
        public uint UserId { get; set; }
        public string Message { get; set; }
        public DateTime ReceivedOn { get; set; }

        public virtual TwitchChannel Channel { get; set; }
    }
}
