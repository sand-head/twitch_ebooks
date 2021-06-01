using System;
using System.Collections.Generic;

namespace TwitchEbooks.Twitch.Chat.Messages
{
    internal record IrcMessage(Dictionary<string, string> Tags, string Source, string Command, List<string> Parameters);

    public abstract record TwitchMessage
    {
        /// <summary>
        /// Represents command "001".
        /// </summary>
        public record Welcome() : TwitchMessage;
        /// <summary>
        /// Represents command "375".
        /// </summary>
        public record MotdStart() : TwitchMessage;
        /// <summary>
        /// Represents command "372".
        /// </summary>
        public record Motd(string Message) : TwitchMessage;
        /// <summary>
        /// Represents command "376".
        /// </summary>
        public record EndOfMotd() : TwitchMessage;
        /// <summary>
        /// Represents command "353".
        /// </summary>
        public record NameReply(List<string> Users) : TwitchMessage;
        /// <summary>
        /// Represents command "366".
        /// </summary>
        public record EndOfNames() : TwitchMessage;
        /// <summary>
        /// Represents command "421".
        /// </summary>
        public record UnknownCommand(string Message) : TwitchMessage;

        public record Ping(string Server) : TwitchMessage;
        /// <summary>
        /// Represents the "CAP * ACK" response indicating a successful capacity request.
        /// </summary>
        public record CapAck(string Capability) : TwitchMessage;
        public record ClearMsg(
            string Login,
            Guid TargetMessageId,
            string Channel,
            string Message) : TwitchMessage;
        public record RoomState(
            bool EmoteOnly,
            bool FollowersOnly,
            bool R9K,
            int Slow,
            bool SubsOnly) : TwitchMessage;
        public record Join(
            string Channel,
            string Username) : TwitchMessage;
        public record Part(
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
