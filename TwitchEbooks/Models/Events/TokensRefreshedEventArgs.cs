using System;
using TwitchEbooks.Database.Models;

namespace TwitchEbooks.Models.Events
{
    public class TokensRefreshedEventArgs : EventArgs
    {
        public TokensRefreshedEventArgs(UserAccessToken tokens) : base()
        {
            NewTokens = tokens;
        }

        public UserAccessToken NewTokens { get; set; }
    }
}
