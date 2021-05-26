﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchEbooks2.Models
{
    public class ValidateResponse
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        public string Login { get; set; }
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
        public List<string> Scope { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
