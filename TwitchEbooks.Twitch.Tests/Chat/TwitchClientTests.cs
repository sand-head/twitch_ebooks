using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Twitch.Chat;
using TwitchEbooks.Twitch.Chat.Messages;
using Xunit;
using Xunit.Abstractions;

namespace TwitchEbooks.Twitch.Tests.Chat
{
    public class TwitchClientTests
    {
        private readonly ITestOutputHelper _output;

        public TwitchClientTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ConnectAsyncShouldSuccessfullyConnectToTwitch()
        {
            // Arrange
            var client = new TwitchClient(_output.BuildLoggerFor<TwitchClient>());
            TwitchMessage.Welcome welcome = null;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            async Task ReadLoop()
            {
                while (client.IsConnected && welcome is null)
                {
                    welcome = await client.ReadMessageAsync<TwitchMessage.Welcome>(token: cts.Token);
                }
            }

            // Act
            await client.ConnectAsync("justinfan0227", "");
            await ReadLoop();

            // Assert
            Assert.NotNull(welcome);
        }

        [Fact]
        public async Task JoinChannelShouldSuccessfullyJoinAChannel()
        {
            // Arrange
            var client = new TwitchClient(_output.BuildLoggerFor<TwitchClient>());
            TwitchMessage.Join join = null;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            async Task ReadLoop()
            {
                while (client.IsConnected && join is null)
                {
                    join = await client.ReadMessageAsync<TwitchMessage.Join>(token: cts.Token);
                }
            }

            // Act
            await client.ConnectAsync("fdgttest", "", serverUri: "wss://irc.fdgt.dev");
            var readTask = ReadLoop();

            await client.JoinChannelAsync("fdgt");
            await readTask;

            // Assert
            Assert.NotNull(join);
            Assert.Equal("fdgt", join.Channel);
            Assert.Equal("fdgttest", join.Username);
        }

        [Fact]
        public async Task TwitchClientShouldSupportReceivingGiftSubs()
        {
            // Arrange
            var client = new TwitchClient(_output.BuildLoggerFor<TwitchClient>());
            TwitchMessage.GiftSub giftSub = null;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            async Task ReadLoop()
            {
                while (client.IsConnected && giftSub is null)
                {
                    giftSub = await client.ReadMessageAsync<TwitchMessage.GiftSub>(token: cts.Token);
                }
            }

            // Act
            await client.ConnectAsync("fdgttest", "", serverUri: "wss://irc.fdgt.dev");
            var readTask = ReadLoop();

            await client.JoinChannelAsync("fdgt");
            while (!client.JoinedChannels.Contains("fdgt")) ;
            await client.SendChatMessageAsync("fdgt", "subgift --count 25 --userid 123 --userid2 456 --channelid 789");
            await readTask;

            // Assert
            Assert.NotNull(giftSub);
            Assert.Equal("fdgt", giftSub.Channel);
            Assert.Equal("fdgttest", giftSub.SenderUserName);
        }

        /* This test doesn't work, as fdgt sends back incorrect multi-month data
         * https://github.com/fdgt-apis/api/issues/38#issuecomment-850766421
        [Fact]
        public async Task TwitchClientShouldSupportReceivingMultiMonthGiftSubs()
        {
            // Arrange
            var client = new TwitchClient();
            client.OnLog += (_, e) => _output.WriteLine(e);
            TwitchMessage.GiftSub giftSub = null;
            async Task ReadLoop()
            {
                while (client.IsConnected)
                {
                    var message = await client.ReadMessageAsync();
                    Console.WriteLine("Message received");
                    if (message is TwitchMessage.GiftSub giftSubMsg)
                    {
                        giftSub = giftSubMsg;
                        break;
                    }
                }
            }

            // Act
            await client.ConnectAsync("fdgttest", "", serverUri: "wss://irc.fdgt.dev");
            var readTask = ReadLoop();

            await client.JoinChannelAsync("fdgt");
            while (!client.JoinedChannels.Contains("fdgt")) ;
            await client.SendChatMessageAsync("fdgt", "subgift --months 3 --count 25 --userid 123 --userid2 456 --channelid 789");
            await readTask;

            // Assert
            Assert.NotNull(giftSub);
            Assert.Equal("fdgt", giftSub.Channel);
            Assert.Equal("fdgttest", giftSub.SenderUserName);
            Assert.Equal(3, giftSub.GiftMonths);
        }
        */
    }
}
