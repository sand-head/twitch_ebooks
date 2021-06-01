using System.Text.Json;
using System.Threading.Tasks;
using TwitchEbooks.Twitch.Chat.Messages;
using TwitchEbooks.Twitch.Extensions;
using Xunit;

namespace TwitchEbooks.Twitch.Tests.Extensions
{
    public class IrcMessageExtensionsTests
    {
        [Fact]
        public void ToTwitchMessageShouldParseJoinMessages()
        {
            // Arrange
            var ircMessage = JsonSerializer.Deserialize<IrcMessage>("{\"Tags\":{},\"Source\":\"user1!user1@user1.tmi.twitch.tv\",\"Command\":\"JOIN\",\"Parameters\":[\"#robo_caller\"]}");

            // Act
            var chat = ircMessage.ToTwitchMessage();

            // Assert
            Assert.NotNull(chat);
            Assert.IsType<TwitchMessage.Join>(chat);
            Assert.Equal("robo_caller", (chat as TwitchMessage.Join).Channel);
        }

        [Fact]
        public void ToTwitchMessageShouldParseChatMessages()
        {
            // Arrange
            var ircMessage = JsonSerializer.Deserialize<IrcMessage>("{\"Tags\":{\"badge-info\":\"subscriber/19\",\"badges\":\"subscriber/12,premium/1\",\"color\":\"\",\"display-name\":\"user1\",\"emotes\":\"300737210:11-18/300737204:20-27\",\"flags\":\"\",\"id\":\"d9fdaa5d-a240-4c36-a3a4-433470bb2d49\",\"mod\":\"0\",\"msg-id\":\"highlighted-message\",\"room-id\":\"10817445\",\"subscriber\":\"1\",\"tmi-sent-ts\":\"1567552165005\",\"turbo\":\"0\",\"user-id\":\"187815264\",\"user-type\":\"\"},\"Source\":\"user1!user1@user1.tmi.twitch.tv\",\"Command\":\"PRIVMSG\",\"Parameters\":[\"#itmejp\",\"hello chat :itmejpM1: :itmejpM3: , just testing this highlight popup to see how glitzy this post will get\"]}");

            // Act
            var chat = ircMessage.ToTwitchMessage();

            // Assert
            Assert.NotNull(chat);
            Assert.IsType<TwitchMessage.Chat>(chat);
            Assert.True((chat as TwitchMessage.Chat).IsHighlighted);
        }
    }
}
