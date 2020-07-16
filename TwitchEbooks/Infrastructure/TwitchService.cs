using Anybotty.StreamClientLibrary.Twitch;
using Anybotty.StreamClientLibrary.Twitch.Models;
using System;
using System.Threading.Tasks;

namespace TwitchEbooks.Infrastructure
{
    public class TwitchService
    {
        private readonly TwitchApiClient _apiClient;
        private readonly TwitchClient _client;

        public TwitchService(TwitchApiClient apiClient)
        {
            _apiClient = apiClient;
            _client = new TwitchClient(apiClient);
        }

        public async Task ConnectAsync(string accessToken = null)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var validationResult = await _apiClient.VerifyAccessTokenAsync(accessToken);
            if (validationResult == null)
            {
                // todo: refresh tokens a couple of times until we get good new ones
            }

            await _client.ConnectAsync(new TwitchConnection(validationResult.Login, accessToken));
        }
    }
}
