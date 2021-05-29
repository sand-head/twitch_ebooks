using System;
using System.Collections.Generic;

namespace TwitchEbooks.Twitch.Chat.Messages
{
    internal record IrcMessage(Dictionary<string, string> Tags, string Source, string Command, List<string> Parameters);

    public abstract record TwitchMessage
    {
        public record Welcome : TwitchMessage;
        public record Ping : TwitchMessage;
        public record Join(
            string Channel,
            string Username) : TwitchMessage;
        public record Leave(
            string Channel,
            string Username) : TwitchMessage;
        public record Chat(
            int Bits,
            string Channel,
            Guid Id,
            bool IsBroadcaster,
            bool IsHighlighted,
            bool IsMe,
            bool IsModerator,
            string Message,
            uint RoomId,
            uint UserId,
            string Username) : TwitchMessage;
        public record GiftSub(
            string Channel,
            int CumulativeMonths,
            int GiftMonths,
            string Message,
            string RecipientDisplayName,
            uint RecipientId,
            string RecipientUserName,
            uint RoomId,
            string SenderDisplayName,
            uint SenderId,
            string SenderUserName) : TwitchMessage;
    }
}
