using System;
using TwitchEbooks.Twitch.Chat.Messages;

namespace TwitchEbooks.Twitch.Extensions
{
    internal static class IrcMessageExtensions
    {
        public static TwitchMessage ToTwitchMessage(this IrcMessage ircMessage)
        {
            return ircMessage.Command switch
            {
                "001" => new TwitchMessage.Welcome(),
                "PING" => new TwitchMessage.Ping(),
                "JOIN" => new TwitchMessage.Join(
                    Channel: ircMessage.Parameters[0][1..],
                    Username: ircMessage.Source[..ircMessage.Source.IndexOf('!')]),
                "PRIVMSG" => ToChatMessage(ircMessage),
                "USERNOTICE" => ircMessage.Tags["msg-id"] switch
                {
                    "subgift" or "anonsubgift" => new TwitchMessage.GiftSub(
                        Channel: ircMessage.Parameters[0][1..],
                        CumulativeMonths: int.Parse(ircMessage.Tags["msg-param-months"]),
                        GiftMonths: ircMessage.Tags.ContainsKey("msg-param-gift-months")
                            ? int.Parse(ircMessage.Tags["msg-param-gift-months"])
                            : 1,
                        Message: ircMessage.Tags["system-msg"].Replace("\\s", " "),
                        RecipientDisplayName: ircMessage.Tags["msg-param-recipient-display-name"],
                        RecipientId: uint.Parse(ircMessage.Tags["msg-param-recipient-id"]),
                        RecipientUserName: ircMessage.Tags["msg-param-recipient-user-name"],
                        RoomId: uint.Parse(ircMessage.Tags["room-id"]),
                        SenderDisplayName: ircMessage.Tags["display-name"],
                        SenderId: uint.Parse(ircMessage.Tags["room-id"]),
                        SenderUserName: ircMessage.Tags["login"]),
                    _ => null
                },
                _ => null
            };
        }

        private static TwitchMessage.Chat ToChatMessage(IrcMessage ircMessage)
        {
            var channelName = ircMessage.Parameters[0][1..];
            var username = ircMessage.Source[..ircMessage.Source.IndexOf('!')];
            var isMeCommand = ircMessage.Parameters[1].StartsWith("\u0001ACTION ") && ircMessage.Parameters[1].EndsWith("\u0001");

            return new TwitchMessage.Chat(
                Bits: ircMessage.Tags.ContainsKey("bits")
                    ? int.Parse(ircMessage.Tags["bits"])
                    : 0,
                Channel: channelName,
                Id: Guid.Parse(ircMessage.Tags["id"]),
                IsBroadcaster: channelName == username,
                IsHighlighted: ircMessage.Tags.ContainsKey("msg-id") && ircMessage.Tags["msg-id"] == "highlighted-message",
                IsMe: isMeCommand,
                IsModerator: bool.Parse(ircMessage.Tags["mod"]),
                Message: isMeCommand ? ircMessage.Parameters[1].Trim('\u0001')[7..] : ircMessage.Parameters[1],
                RoomId: uint.Parse(ircMessage.Tags["room-id"]),
                UserId: uint.Parse(ircMessage.Tags["user-id"]),
                Username: username);
        }
    }
}
