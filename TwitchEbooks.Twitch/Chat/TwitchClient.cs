﻿using System;
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

        private Uri _serverUri;
        private string _username, _accessToken;
        private List<string> _joinedChannels;

        private Task _messageReadLoop;

        public TwitchClient()
        {
            _client = new ClientWebSocket();
            _tokenSource = new CancellationTokenSource();
            _incomingMessageQueue = Channel.CreateUnbounded<TwitchMessage>(new UnboundedChannelOptions
            {
                SingleWriter = true
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
                while (IsConnected && !_tokenSource.Token.IsCancellationRequested)
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
                        foreach (var message in messages.Trim().Split('\n'))
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
                            if (twitchMessage is TwitchMessage.Ping)
                                await SendRawMessageAsync("PONG");
                            else if (twitchMessage is TwitchMessage.Join joinMsg && _username == joinMsg.Username)
                                _joinedChannels.Add(joinMsg.Channel);
                            else if (twitchMessage is TwitchMessage.Leave leaveMsg && _username == leaveMsg.Username)
                                _joinedChannels.Remove(leaveMsg.Channel);

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
    }
}
