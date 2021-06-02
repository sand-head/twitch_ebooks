using MediatR;
using TwitchEbooks.Twitch.Chat.Messages;

namespace TwitchEbooks.Models.MediatR.Notifications
{
    public record GenerateMessageNotification(uint ChannelId) : INotification;
    public record IgnoreUserNotification(uint ChannelId, uint UserId) : INotification;
    public record PurgeWordNotification(uint ChannelId, string Word) : INotification;
    public record ReceiveMessageNotification(TwitchMessage.Chat Message) : INotification;
}
