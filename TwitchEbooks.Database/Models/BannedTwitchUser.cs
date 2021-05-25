using System;

namespace TwitchEbooks.Database.Models
{
    public class BannedTwitchUser
    {
        public uint Id { get; set; }
        public uint ChannelId { get; set; }
        public DateTime BannedOn { get; set; }

        public virtual TwitchChannel Channel { get; set; }
    }
}
