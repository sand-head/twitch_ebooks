using System;
using System.Linq;
using System.Threading.Tasks;
using TwitchEbooks.Twitch.Chat;
using TwitchEbooks.Twitch.Chat.Messages;
using Xunit;

namespace TwitchEbooks.Twitch.Tests.Chat
{
    public class TwitchClientTests
    {
        [Fact]
        public async Task JoinChannelShouldSuccessfullyJoinAChannel()
        {
            // Arrange
            var client = new TwitchClient();
            TwitchMessage.Join join = null;
            async Task ReadLoop()
            {
                while (client.IsConnected)
                {
                    var message = await client.ReadMessageAsync();
                    Console.WriteLine("Message received");
                    if (message is TwitchMessage.Join joinMsg)
                    {
                        join = joinMsg;
                        break;
                    }
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
            var client = new TwitchClient();
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
