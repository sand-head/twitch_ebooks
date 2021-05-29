using MediatR;
using TwitchEbooks.Twitch.Chat.Messages;

namespace TwitchEbooks.Models.Notifications
{
    public record GenerateMessageNotification(uint ChannelId) : INotification;
    public record IgnoreUserNotification(uint ChannelId, uint UserId) : INotification;
    public record JoinNotification(uint ChannelId) : INotification;
    public record LeaveNotification(uint ChannelId) : INotification;
    public record PurgeWordNotification(uint ChannelId, string Word) : INotification;
    public record ReceiveMessageNotification(TwitchMessage.Chat Message) : INotification;
    public record SendMessageNotification(uint ChannelId, string Message) : INotification;
}
