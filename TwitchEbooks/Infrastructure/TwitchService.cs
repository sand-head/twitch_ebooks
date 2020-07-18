using Anybotty.StreamClientLibrary.Common.Models.Messages;
using Anybotty.StreamClientLibrary.Twitch;
using Anybotty.StreamClientLibrary.Twitch.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models;
using TwitchEbooks.Models.Events;

namespace TwitchEbooks.Infrastructure
{
    public class TwitchService
    {
        private readonly ILogger<TwitchService> _logger;
        private readonly TwitchApiClient _apiClient;
        private readonly TwitchSettings _twitchSettings;
        private readonly TwitchClient _client;
        private UserAccessToken _userTokens;
        private string _channelName;

        public TwitchService(ILogger<TwitchService> logger, TwitchSettings twitchSettings, TwitchApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
            _twitchSettings = twitchSettings;

            _client = new TwitchClient(apiClient);
            _client.Log += _logger.LogInformation;
            _client.Connected += TwitchClient_Connected;
            _client.MessageReceived += TwitchClient_MessageReceived;
            _userTokens = null;
            _channelName = null;
        }

        public event EventHandler OnConnected;
        public event EventHandler<TokensRefreshedEventArgs> OnTokensRefreshed;
        public event EventHandler<GenerationRequestReceivedEventArgs> OnGenerationRequestReceived;
        public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;

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
            _channelName = validationResult.Login;
            await _client.ConnectAsync(new TwitchConnection(validationResult.Login, tokens.AccessToken));
        }

        public async Task JoinChannelByIdAsync(uint channelId)
        {
            if (_userTokens == null) throw new Exception("JoinChannelById must be called after ConnectAsync.");

            var user = await _apiClient.GetUserAsync(_userTokens.AccessToken, _twitchSettings.ClientId, channelId.ToString());
            if (user == null) throw new Exception("GetUserAsync returned null, which means we probably need to refresh tokens but I don't wanna do that right now.");

            _client.JoinChannel(user.Login);
        }

        public string BuildAuthCodeFlowUrl(string[] scopes) =>
            $"https://id.twitch.tv/oauth2/authorize?client_id={WebUtility.UrlEncode(_twitchSettings.ClientId)}"
                + $"&redirect_uri={WebUtility.UrlEncode(_twitchSettings.RedirectUri)}&response_type=code&scope={WebUtility.UrlEncode(string.Join(' ', scopes))}";

        private void HandleChatMessage(ChatMessage message)
        {
            if (message.Message.StartsWith("~generate"))
            {
                _logger.LogInformation("Generation request received for channel {ChannelId}.", message.RoomId);
                OnGenerationRequestReceived?.Invoke(this, new GenerationRequestReceivedEventArgs
                {
                    ChannelId = message.RoomId,
                    ChannelName = message.ChannelName
                });
                return;
            }

            OnChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
            {
                Message = message
            });
        }

        private void TwitchClient_Connected()
        {
            _logger.LogInformation("Twitch client connected!");
            OnConnected?.Invoke(this, null);
            _client.JoinChannel(_channelName);
        }

        private void TwitchClient_MessageReceived(StreamMessage message)
        {
            if (message is ChatMessage chatMessage)
            {
                if (chatMessage.RoomId == _userTokens.UserId)
                {
                    // do specific commands for the main channel (like ~join) here
                }
                else HandleChatMessage(chatMessage);
            }
            // todo: maybe do a fun generation when receiving a gift sub?
        }
    }
}
