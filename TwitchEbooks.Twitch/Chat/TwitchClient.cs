using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Twitch.Chat.EventArgs;
using TwitchEbooks.Twitch.Chat.Messages;
using TwitchEbooks.Twitch.Extensions;

namespace TwitchEbooks.Twitch.Chat
{
    /// <summary>
    /// A TMI (Twitch Messaging Interface) client using WebSockets.
    /// </summary>
    public class TwitchClient
    {
        private readonly ClientWebSocket _client;
        private readonly CancellationTokenSource _tokenSource;

        private Uri _serverUri;
        private string _username, _accessToken;
        private List<string> _joinedChannels;

        public TwitchClient()
        {
            _client = new ClientWebSocket();
            _tokenSource = new CancellationTokenSource();
        }

        public bool IsConnected => _client.State == WebSocketState.Open;
        public IReadOnlyList<string> JoinedChannels => _joinedChannels;

        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
        public event EventHandler<string> OnLog;

        public async Task ConnectAsync(string username, string accessToken, string serverUri = "wss://irc-ws.chat.twitch.tv:443", CancellationToken token = default)
        {
            _serverUri = new Uri(serverUri);
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _joinedChannels = new List<string>();

            using var comboToken = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token);
            await _client.ConnectAsync(_serverUri, comboToken.Token);
            await SendRawMessageAsync($"PASS oauth:{accessToken}");
            await SendRawMessageAsync($"NICK {username}");
            await SendRawMessageAsync("CAP REQ :twitch.tv/tags");
            await SendRawMessageAsync("CAP REQ :twitch.tv/commands");
            await SendRawMessageAsync("CAP REQ :twitch.tv/membership");
            // OnConnected?.Invoke();
        }

        public async Task ReconnectAsync(string accessToken = null, CancellationToken token = default)
        {
            await ConnectAsync(_username, accessToken ?? _accessToken, _serverUri.ToString(), token);
        }

        public async Task JoinChannelAsync(string channelName)
        {
            if (IsConnected)
                await SendRawMessageAsync($"JOIN #{channelName}");
        }

        public async Task LeaveChannelAsync(string channelName)
        {
            if (IsConnected)
                await SendRawMessageAsync($"PART #{channelName}");
        }

        public async Task SendChatMessageAsync(string channelName, string message)
        {
            if (IsConnected)
                await SendRawMessageAsync($"PRIVMSG #{channelName} :{message}");
        }

        public async Task SendRawMessageAsync(string message)
        {
            try
            {
                if (IsConnected && message != null)
                {
                    // OnLog?.Invoke("Sent: {message}", message);
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await _client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _tokenSource.Token);
                }
            }
            catch (Exception e)
            {
                OnLog?.Invoke(this, $"Exception occured when sending WebSocket message {message}: {e.Message}");
            }
        }

        public async Task<TMessage> ReadMessageAsync<TMessage>(CancellationToken token = default) where TMessage : TwitchMessage
        {
            using var comboToken = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token);

            while (IsConnected && !comboToken.Token.IsCancellationRequested)
            {
                var message = await ReadMessageAsync(token);
                if (message is TMessage tMessage)
                    return tMessage;
            }

            return null;
        }

        public async Task<TwitchMessage> ReadMessageAsync(CancellationToken token = default)
        {
            var message = "";
            using var comboToken = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token);

            while (IsConnected && !comboToken.Token.IsCancellationRequested)
            {
                var buffer = new byte[1024];
                var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), comboToken.Token);

                if (result is null || result.MessageType == WebSocketMessageType.Binary) continue;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    message += Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Disconnect();
                }

                if (result.EndOfMessage)
                {
                    var twitchMessage = IrcMessageParser.TryParse(message, out var ircMessage)
                        ? ircMessage.ToTwitchMessage()
                        : throw new Exception("Could not parse IrcMessage from received input.");

                    OnLog?.Invoke(this, $"Received: {twitchMessage}");
                    // do some fun things internally so consumers don't have to deal with them
                    if (twitchMessage is TwitchMessage.Ping)
                        await SendRawMessageAsync("PONG");
                    else if (twitchMessage is TwitchMessage.Join joinMsg && _username == joinMsg.Username)
                        _joinedChannels.Add(joinMsg.Channel);
                    else if (twitchMessage is TwitchMessage.Leave leaveMsg && _username == leaveMsg.Username)
                        _joinedChannels.Remove(leaveMsg.Channel);

                    if (twitchMessage is not null) return twitchMessage;

                    OnLog?.Invoke(this, $"Received weird message: {message}");
                    message = "";
                }
            }

            return null;
        }

        public void Disconnect()
        {
            _client.Abort();
            OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs());
        }

        public void Dispose()
        {
            Disconnect();
            _tokenSource.Cancel();
            Thread.Sleep(500);
            _tokenSource.Dispose();
            _client.Dispose();
            GC.Collect();
        }
    }
}
