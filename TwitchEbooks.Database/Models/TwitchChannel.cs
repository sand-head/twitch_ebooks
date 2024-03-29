﻿using System.Collections.Generic;

namespace TwitchEbooks.Database.Models
{
    public class TwitchChannel
    {
        public uint Id { get; set; }

        public virtual List<TwitchMessage> Messages { get; set; }
        public virtual List<IgnoredTwitchUser> IgnoredUsers { get; set; }
    }
}
