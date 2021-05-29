using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchEbooks.Twitch.Api.Responses
{
    public class User
    {
        public string Id { get; set; }
        public string Login { get; set; }
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
        public string Type { get; set; }
        [JsonPropertyName("broadcaster_type")]
        public string BroadcasterType { get; set; }
        public string Description { get; set; }
        [JsonPropertyName("profile_image_url")]
        public string ProfileImageUrl { get; set; }
        [JsonPropertyName("offline_image_url")]
        public string OfflineImageUrl { get; set; }
        [JsonPropertyName("view_count")]
        public int ViewCount { get; set; }
        public string Email { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class UsersResponse
    {
        [JsonPropertyName("data")]
        public User[] Users { get; set; }
    }
}
