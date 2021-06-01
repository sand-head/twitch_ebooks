using ComposableAsync;
using RateLimiter;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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
        private readonly Channel<TwitchMessage> _incomingMessageQueue;
        private readonly Channel<(string channelName, string message)> _outgoingMessageQueue;
        private readonly Channel<string> _joinQueue;

        private Uri _serverUri;
        private string _username, _accessToken;
        private List<string> _joinedChannels;

        private Task _messageReadLoop, _messageSendLoop, _joinLoop;

        public TwitchClient()
        {
            _client = new ClientWebSocket();
            _tokenSource = new CancellationTokenSource();

            _incomingMessageQueue = Channel.CreateUnbounded<TwitchMessage>(new UnboundedChannelOptions
            {
                SingleWriter = true
            });
            _outgoingMessageQueue = Channel.CreateBounded<(string channelName, string message)>(new BoundedChannelOptions(30)
            {
                SingleReader = true
            });
            _joinQueue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
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
            _messageReadLoop = MessageReadLoop();
            _messageSendLoop = MessageSendLoop();
            _joinLoop = JoinLoop();
        }

        public async Task ReconnectAsync(string accessToken = null, CancellationToken token = default)
        {
            await ConnectAsync(_username, accessToken ?? _accessToken, _serverUri.ToString(), token);
        }

        public async ValueTask JoinChannelAsync(string channelName, CancellationToken token = default)
        {
            await _joinQueue.Writer.WriteAsync(channelName, token);
        }

        public async Task LeaveChannelAsync(string channelName)
        {
            if (IsConnected)
                await SendRawMessageAsync($"PART #{channelName}");
        }

        public async ValueTask SendChatMessageAsync(string channelName, string message, CancellationToken token = default)
        {
            await _outgoingMessageQueue.Writer.WriteAsync((channelName, message), token);
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

        public async Task<TMessage> ReadMessageAsync<TMessage>(Predicate<TMessage> predicate = default, CancellationToken token = default) where TMessage : TwitchMessage
        {
            using var comboToken = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token);

            while (IsConnected && !comboToken.Token.IsCancellationRequested)
            {
                var message = await _incomingMessageQueue.Reader.ReadAsync(token);
                if (message is TMessage tMessage && (predicate is null || predicate(tMessage)))
                    return tMessage;
            }

            return null;
        }

        public async ValueTask<TwitchMessage> ReadMessageAsync(CancellationToken token = default)
        {
            return await _incomingMessageQueue.Reader.ReadAsync(token);
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

        private async Task MessageReadLoop()
        {
            var messages = "";

            try
            {
                while (IsConnected && !_tokenSource.IsCancellationRequested)
                {
                    var buffer = new byte[1024];
                    var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), _tokenSource.Token);

                    if (result is null || result.MessageType == WebSocketMessageType.Binary) continue;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messages += Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Disconnect();
                    }

                    if (result.EndOfMessage)
                    {
                        foreach (var message in messages.Trim().Replace("\r", string.Empty).Split('\n'))
                        {
                            var twitchMessage = IrcMessageParser.TryParse(message, out var ircMessage)
                                ? ircMessage.ToTwitchMessage()
                                : null;

                            if (twitchMessage is null)
                            {
                                OnLog?.Invoke(this, $"Received weird message: {message}");
                                continue;
                            }

                            OnLog?.Invoke(this, $"Received: {twitchMessage}");
                            // do some fun things internally so consumers don't have to deal with them
                            if (twitchMessage is TwitchMessage.Ping ping)
                                await SendRawMessageAsync($"PONG :{ping.Server}");
                            else if (twitchMessage is TwitchMessage.Join joinMsg && _username == joinMsg.Username)
                                _joinedChannels.Add(joinMsg.Channel);
                            else if (twitchMessage is TwitchMessage.Part partMsg && _username == partMsg.Username)
                                _joinedChannels.Remove(partMsg.Channel);

                            await _incomingMessageQueue.Writer.WriteAsync(twitchMessage, _tokenSource.Token);
                        }
                        messages = "";
                    }
                }
            }
            catch (Exception e)
            {
                OnLog?.Invoke("Exception occured in client message reading loop: {Message}", e.Message);
            }

            OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs());
        }

        private async Task MessageSendLoop()
        {
            var rateLimit = TimeLimiter.GetFromMaxCountByInterval(20, TimeSpan.FromSeconds(30));

            try
            {
                while (IsConnected && !_tokenSource.IsCancellationRequested)
                {
                    await rateLimit;
                    var (channelName, message) = await _outgoingMessageQueue.Reader.ReadAsync(_tokenSource.Token);
                    await SendRawMessageAsync($"PRIVMSG #{channelName} :{message}");
                }
            }
            catch (Exception e)
            {
                OnLog?.Invoke("Exception occured in client message sending loop: {Message}", e.Message);
            }
        }

        private async Task JoinLoop()
        {
            var rateLimit = TimeLimiter.GetFromMaxCountByInterval(20, TimeSpan.FromSeconds(10));

            try
            {
                while (IsConnected && !_tokenSource.IsCancellationRequested)
                {
                    await rateLimit;
                    var channelName = await _joinQueue.Reader.ReadAsync(_tokenSource.Token);
                    await SendRawMessageAsync($"JOIN #{channelName}");
                }
            }
            catch (Exception e)
            {
                OnLog?.Invoke("Exception occured in client channel join loop: {Message}", e.Message);
            }
        }
    }
}
