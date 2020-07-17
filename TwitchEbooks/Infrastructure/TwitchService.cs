using Anybotty.StreamClientLibrary.Twitch;
using Anybotty.StreamClientLibrary.Twitch.Models;
using System;
using System.Threading.Tasks;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models;
using TwitchEbooks.Models.Events;

namespace TwitchEbooks.Infrastructure
{
    public class TwitchService
    {
        private readonly TwitchApiClient _apiClient;
        private readonly TwitchSettings _twitchSettings;
        private readonly TwitchClient _client;
        private UserAccessToken _userTokens;

        public TwitchService(TwitchApiClient apiClient, TwitchSettings twitchSettings)
        {
            _apiClient = apiClient;
            _twitchSettings = twitchSettings;

            _client = new TwitchClient(apiClient);
            _userTokens = null;
        }

        public event EventHandler<TokensRefreshedEventArgs> OnTokensRefreshed;

        public async Task ConnectAsync(UserAccessToken tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));

            var validationResult = await _apiClient.VerifyAccessTokenAsync(tokens.AccessToken);
            if (validationResult == null)
            {
                var newTokens = await _apiClient.RefreshTokensAsync(tokens.RefreshToken, _twitchSettings.ClientId, _twitchSettings.ClientSecret);
                tokens = new UserAccessToken
                {
                    Id = Guid.NewGuid(),
                    UserId = tokens.UserId,
                    AccessToken = newTokens.AccessToken,
                    RefreshToken = newTokens.RefreshToken,
                    ExpiresIn = newTokens.ExpiresIn,
                    CreatedOn = DateTime.UtcNow,
                };
                OnTokensRefreshed?.Invoke(this, new TokensRefreshedEventArgs(tokens));
                validationResult = await _apiClient.VerifyAccessTokenAsync(tokens.AccessToken);
            }

            _userTokens = tokens;
            await _client.ConnectAsync(new TwitchConnection(validationResult.Login, tokens.AccessToken));
        }

        public async Task JoinChannelByIdAsync(uint channelId)
        {
            if (_userTokens == null) throw new Exception("JoinChannelById must be called after ConnectAsync.");

            var user = await _apiClient.GetUserAsync(_userTokens.AccessToken, _twitchSettings.ClientId, channelId.ToString());
            if (user == null) throw new Exception("GetUserAsync returned null, which means we probably need to refresh tokens but I don't wanna do that right now.");

            _client.JoinChannel(user.Login);
        }
    }
}
