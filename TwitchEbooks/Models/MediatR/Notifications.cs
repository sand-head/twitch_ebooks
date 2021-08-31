using MediatR;
using System;
using TwitchEbooks.Twitch.Chat.Messages;

namespace TwitchEbooks.Models.MediatR.Notifications
{
    public record DeleteMessageNotification(Guid MessageId) : INotification;
    public record GenerateMessageNotification(uint ChannelId, Guid? MessageId = null) : INotification;
    public record IgnoreUserNotification(uint ChannelId, uint UserId) : INotification;
    public record PurgeWordNotification(uint ChannelId, string Word) : INotification;
    public record ReceiveMessageNotification(TwitchMessage.Chat Message) : INotification;
}
