using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using TwitchEbooks.Twitch.Api.Responses;

namespace TwitchEbooks.Twitch.Api
{
    public class TwitchApi
    {
        private readonly HttpClient _client;

        public TwitchApi(HttpClient client)
        {
            _client = client;
        }

        public async Task<ValidateResponse> VerifyAccessTokenAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
            request.Headers.Add("Authorization", $"OAuth {accessToken}");
            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ValidateResponse>();
        }

        public async Task<AuthenticationResponse> RefreshTokensAsync(string refreshToken, string clientId, string clientSecret)
        {
            var response = await _client.PostAsync("https://id.twitch.tv/oauth2/token" +
                "?grant_type=refresh_token" +
                $"&refresh_token={HttpUtility.UrlEncode(refreshToken)}" +
                $"&client_id={clientId}" +
                $"&client_secret={clientSecret}", null);
            return await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        }

        public async Task<UsersResponse> GetUsersAsync(string accessToken, string clientId, List<string> ids = default, List<string> logins = default)
        {
            var queryStringList = new List<string>();
            foreach (var id in ids)
            {
                queryStringList.Add($"id={id}");
            }
            foreach (var login in logins)
            {
                queryStringList.Add($"login={login}");
            }

            var queryString = string.Join('&', queryStringList);
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.twitch.tv/helix/users?{queryString}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("Client-ID", clientId);
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UsersResponse>();
        }
    }
}
