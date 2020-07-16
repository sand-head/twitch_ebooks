using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchEbooks.Database.Models
{
    public class UserAccessToken
    {
        public uint UserId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
