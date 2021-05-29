using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchEbooks.Twitch.Api.Responses
{
    public class AuthenticationResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        public List<string> Scope { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}
