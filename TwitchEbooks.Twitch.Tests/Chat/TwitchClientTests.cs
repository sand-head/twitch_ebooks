using System;
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
    }
}
