using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchEbooks.Database.Models;

namespace TwitchEbooks.Database
{
    public class TwitchEbooksContext : DbContext
    {
        public TwitchEbooksContext(DbContextOptions options) : base(options) { }

        public DbSet<TwitchChannel> Channels { get; set; }
        public DbSet<TwitchMessage> Messages { get; set; }
        public DbSet<UserAccessToken> AccessTokens { get; set; }
    }
}
