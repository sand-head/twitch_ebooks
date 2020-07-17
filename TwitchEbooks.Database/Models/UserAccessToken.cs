using System;

namespace TwitchEbooks.Database.Models
{
    public class UserAccessToken
    {
        public Guid Id { get; set; }
        public uint UserId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
