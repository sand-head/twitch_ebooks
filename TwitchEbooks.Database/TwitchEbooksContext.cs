using Microsoft.EntityFrameworkCore;
using TwitchEbooks.Database.Models;

namespace TwitchEbooks.Database
{
    public class TwitchEbooksContext : DbContext
    {
        public TwitchEbooksContext(DbContextOptions options) : base(options) { }

        public DbSet<BannedTwitchUser> BannedTwitchUsers { get; set; }
        public DbSet<TwitchChannel> Channels { get; set; }
        public DbSet<TwitchMessage> Messages { get; set; }
        public DbSet<UserAccessToken> AccessTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BannedTwitchUser>()
                .HasKey(b => new { b.Id, b.ChannelId });
        }
    }
}
