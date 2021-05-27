using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchEbooks.Database.Models
{
    public class IgnoredTwitchUser
    {
        public uint Id { get; set; }
        public uint ChannelId { get; set; }
        public DateTime BannedOn { get; set; }

        public virtual TwitchChannel Channel { get; set; }
    }
}
