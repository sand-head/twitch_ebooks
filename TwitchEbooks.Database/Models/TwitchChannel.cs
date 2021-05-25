using System.Collections.Generic;

namespace TwitchEbooks.Database.Models
{
    public class TwitchChannel
    {
        public uint Id { get; set; }

        public virtual List<TwitchMessage> Messages { get; set; }
        public virtual List<BannedTwitchUser> BannedUsers { get; set; }
    }
}
