using System;
using System.Text.Json;
using TwitchEbooks.Twitch.Chat;
using Xunit;
using Xunit.Abstractions;

namespace TwitchEbooks.Twitch.Tests.Chat
{
    public class IrcMessageParserTests
    {
        private readonly ITestOutputHelper _output;
        
        public IrcMessageParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TryParseShouldParseHighlightedMessageAndReturnTrue()
        {
            // Arrange
            var message = "@badge-info=subscriber/19;badges=subscriber/12,premium/1;color=;display-name=user1;emotes=300737210:11-18/300737204:20-27;flags=;id=d9fdaa5d-a240-4c36-a3a4-433470bb2d49;mod=0;msg-id=highlighted-message;room-id=10817445;subscriber=1;tmi-sent-ts=1567552165005;turbo=0;user-id=187815264;user-type= :user1!user1@user1.tmi.twitch.tv PRIVMSG #itmejp :hello chat :itmejpM1: :itmejpM3: , just testing this highlight popup to see how glitzy this post will get";

            // Act
            var success = IrcMessageParser.TryParse(message, out var ircMessage);
            _output.WriteLine(JsonSerializer.Serialize(ircMessage));

            // Assert
            Assert.True(success);
            Assert.NotEmpty(ircMessage.Tags);
            Assert.Equal(15, ircMessage.Tags.Count);
            Assert.Equal("d9fdaa5d-a240-4c36-a3a4-433470bb2d49", ircMessage.Tags["id"]);
            Assert.Equal("subscriber/12,premium/1", ircMessage.Tags["badges"]);
            Assert.Equal("300737210:11-18/300737204:20-27", ircMessage.Tags["emotes"]);
            Assert.Equal("user1!user1@user1.tmi.twitch.tv", ircMessage.Source);
            Assert.NotEmpty(ircMessage.Parameters);
            Assert.Equal(2, ircMessage.Parameters.Count);
            Assert.Equal("#itmejp", ircMessage.Parameters[0]);
        }

        [Fact]
        public void TryParseShouldParseJoinMessageAndReturnTrue()
        {
            // Arrange
            var message = ":user1!user1@user1.tmi.twitch.tv JOIN #robo_caller";

            // Act
            var success = IrcMessageParser.TryParse(message, out var ircMessage);
            _output.WriteLine(JsonSerializer.Serialize(ircMessage));

            // Assert
            Assert.True(success);
            Assert.Empty(ircMessage.Tags);
            Assert.Equal("user1!user1@user1.tmi.twitch.tv", ircMessage.Source);
            Assert.NotEmpty(ircMessage.Parameters);
            Assert.Single(ircMessage.Parameters);
            Assert.Equal("#robo_caller", ircMessage.Parameters[0]);
        }

        [Fact]
        public void TryParseShouldParseMessageWithEmptyTrailAndReturnTrue()
        {
            // Arrange
            var message = ":user1!user1@user1.tmi.twitch.tv TEST #robo_caller :";

            // Act
            var success = IrcMessageParser.TryParse(message, out var ircMessage);
            _output.WriteLine(JsonSerializer.Serialize(ircMessage));

            // Assert
            Assert.True(success);
            Assert.Empty(ircMessage.Tags);
            Assert.Equal("user1!user1@user1.tmi.twitch.tv", ircMessage.Source);
            Assert.NotEmpty(ircMessage.Parameters);
            Assert.Equal(2, ircMessage.Parameters.Count);
            Assert.Equal("#robo_caller", ircMessage.Parameters[0]);
            Assert.Empty(ircMessage.Parameters[1]);
        }

        [Fact]
        public void TryParseShouldParsePingMessageAndReturnTrue()
        {
            // Arrange
            var message = "PING :tmi.twitch.tv";

            // Act
            var success = IrcMessageParser.TryParse(message, out var ircMessage);
            _output.WriteLine(JsonSerializer.Serialize(ircMessage));

            // Assert
            Assert.True(success);
            Assert.Empty(ircMessage.Tags);
            Assert.Null(ircMessage.Source);
            Assert.Equal("PING", ircMessage.Command);
            Assert.NotEmpty(ircMessage.Parameters);
            Assert.Single(ircMessage.Parameters);
            Assert.Equal("tmi.twitch.tv", ircMessage.Parameters[0]);
        }
    }
}
