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
            _client.MessageReceived += async (msg) => await TwitchClient_MessageReceived(msg);
            _client.Disconnected += async () => await TwitchClient_Disconnected();
            _userTokens = null;
            _channelName = null;
        }

        public event EventHandler OnConnected;
        public event EventHandler<TokensRefreshedEventArgs> OnTokensRefreshed;
        public event EventHandler<GenerationRequestReceivedEventArgs> OnGenerationRequestReceived;
        public event EventHandler<MessageReceivedEventArgs<ChatMessage>> OnChatMessageReceived;
        public event EventHandler<MessageReceivedEventArgs<ClearMessage>> OnChatMessageDeleted;
        public event EventHandler<PurgeWordRequestReceivedEventArgs> OnPurgeWordRequestReceived;
        public event EventHandler<BanUserRequestReceivedEventArgs> OnBanUserRequestReceived;
        public event EventHandler<MessageReceivedEventArgs<GiftSubscriptionMessage>> OnGiftSubReceived;
        public event EventHandler<BotJoinLeaveReceivedEventArgs> OnBotJoinLeaveReceivedEventArgs;

        public async Task ConnectAsync(UserAccessToken tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            var (validationResult, validTokens) = await ValidateAndRefreshTokens(tokens);

            _userTokens = validTokens;
            _channelName = validationResult.Login;
            await _client.ConnectAsync(new TwitchConnection(validationResult.Login, validTokens.AccessToken));
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

        public async Task SendMessageAsync(string channelName, string message)
        {
            await _client.SendMessageAsync(channelName, message);
        }

        private async Task<(VerifyApiMessage, UserAccessToken)> ValidateAndRefreshTokens(UserAccessToken tokens)
        {
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
            return (validationResult, tokens);
        }

        private async Task HandleBotCommandsAsync(ChatMessage message)
        {
            if (message.Message.StartsWith("~join"))
            {
                if (_client.JoinedChannels.Contains(message.Username))
                {
                    await SendMessageAsync(_channelName, $"@{message.Username} I'm already in your chat, so I can't join again.");
                    return;
                }

                _client.JoinChannel(message.Username);
                await SendMessageAsync(_channelName, $"@{message.Username} Successfully joined your chat!");
                OnBotJoinLeaveReceivedEventArgs?.Invoke(this, new BotJoinLeaveReceivedEventArgs
                {
                    RequestedPresence = BotPresenceRequest.Join,
                    ChannelId = message.UserId,
                    ChannelName = message.Username,
                    BotChannelName = _channelName
                });
            }
            else if (message.Message.StartsWith("~leave"))
            {
                if (!_client.JoinedChannels.Contains(message.Username))
                {
                    await SendMessageAsync(_channelName, $"@{message.Username} I'm not currently in your chat, so I can't leave.");
                    return;
                }

                _client.LeaveChannel(message.Username);
                await SendMessageAsync(_channelName, $"@{message.Username} Successfully left your chat!");
                OnBotJoinLeaveReceivedEventArgs?.Invoke(this, new BotJoinLeaveReceivedEventArgs
                {
                    RequestedPresence = BotPresenceRequest.Leave,
                    ChannelId = message.UserId,
                    ChannelName = message.Username,
                    BotChannelName = _channelName
                });
            }
        }

        private async Task HandleUserCommandsAsync(ChatMessage message)
        {
            if (message.Message.StartsWith("~generate"))
            {
                _logger.LogInformation("Generation request received for channel {ChannelId}.", message.RoomId);
                OnGenerationRequestReceived?.Invoke(this, new GenerationRequestReceivedEventArgs
                {
                    ChannelId = message.RoomId,
                    ChannelName = message.ChannelName
                });
            }
            else if (message.Message.StartsWith("~leave"))
            {
                if (!message.IsBroadcaster)
                {
                    await SendMessageAsync(message.ChannelName, $"@{message.Username} Only the broadcaster can ask me to leave, sorry!");
                    return;
                }

                _client.LeaveChannel(message.Username);
                await SendMessageAsync(message.Username, $"@{message.Username} Successfully left your chat!");
                OnBotJoinLeaveReceivedEventArgs?.Invoke(this, new BotJoinLeaveReceivedEventArgs
                {
                    RequestedPresence = BotPresenceRequest.Leave,
                    ChannelId = message.RoomId,
                    ChannelName = message.Username,
                    BotChannelName = _channelName
                });
            }
            else if (message.Message.StartsWith("~purge"))
            {
                if (!message.IsBroadcaster && !message.IsMod)
                {
                    await SendMessageAsync(message.ChannelName, $"@{message.Username} Only mods can purge words, sorry!");
                    return;
                }

                var splitMsg = message.Message.Split(' ');
                if (splitMsg.Length <= 1 || string.IsNullOrWhiteSpace(splitMsg[1]))
                {
                    await SendMessageAsync(message.ChannelName, $"@{message.Username} You have to include a word to purge!");
                    return;
                }

                OnPurgeWordRequestReceived?.Invoke(this, new PurgeWordRequestReceivedEventArgs
                {
                    ChannelId = message.RoomId,
                    Word = string.Join(' ', splitMsg[1..])
                });
            }
            else if (message.Message.StartsWith("~ban"))
            {
                if (!message.IsBroadcaster && !message.IsMod)
                {
                    await SendMessageAsync(message.ChannelName, $"@{message.Username} Only mods can ban users, sorry!");
                    return;
                }

                var splitMsg = message.Message.Split(' ');
                if (splitMsg.Length <= 1 || string.IsNullOrWhiteSpace(splitMsg[1]))
                {
                    await SendMessageAsync(message.ChannelName, $"@{message.Username} You have to include a username to ban!");
                    return;
                }

                var userName = splitMsg[1].Trim();
                var user = await _apiClient.GetUserAsync(_userTokens.AccessToken, _twitchSettings.ClientId, (UserInputType.Username, userName));
                if (user is null)
                {
                    await SendMessageAsync(message.ChannelName, $"@{message.Username} Couldn't find a user by the name of \"${userName}\", sorry!");
                    return;
                }

                OnBanUserRequestReceived?.Invoke(this, new BanUserRequestReceivedEventArgs
                {
                    ChannelId = message.RoomId,
                    ChannelName = message.ChannelName,
                    UserId = uint.Parse(user.Id)
                });
            }
            else
            {
                OnChatMessageReceived?.Invoke(this, new MessageReceivedEventArgs<ChatMessage>
                {
                    Message = message
                });
            }
        }

        private void TwitchClient_Connected()
        {
            _logger.LogInformation("Twitch client connected!");
            OnConnected?.Invoke(this, null);
            _client.JoinChannel(_channelName);
        }

        private async Task TwitchClient_MessageReceived(StreamMessage message)
        {
            if (message is ChatMessage chatMessage)
            {
                if (chatMessage.RoomId == _userTokens.UserId)
                    await HandleBotCommandsAsync(chatMessage);
                else
                    await HandleUserCommandsAsync(chatMessage);
            }
            else if (message is ClearMessage clearMessage)
            {
                OnChatMessageDeleted?.Invoke(this, new MessageReceivedEventArgs<ClearMessage>
                {
                    Message = clearMessage
                });
            }
            else if (message is GiftSubscriptionMessage giftSubMessage && giftSubMessage.RecipientName.ToLower() == _channelName)
            {
                OnGiftSubReceived?.Invoke(this, new MessageReceivedEventArgs<GiftSubscriptionMessage>
                {
                    Message = giftSubMessage
                });
            }
        }

        private async Task TwitchClient_Disconnected()
        {
            var (validationResult, validTokens) = await ValidateAndRefreshTokens(_userTokens);
            _userTokens = validTokens;
            _channelName = validationResult.Login;
            await _client.ReconnectAsync(new TwitchConnection(validationResult.Login, validTokens.AccessToken));
        }
    }
}
