using MediatR;
using TwitchEbooks.Database.Models;
using TwitchLib.Client.Models;

namespace TwitchEbooks2.Models.Notifications
{
    public record GenerateMessageNotification(uint ChannelId) : INotification;
    public record IgnoreUserNotification(uint ChannelId, uint UserId) : INotification;
    public record JoinNotification(uint ChannelId) : INotification;
    public record LeaveNotification(uint ChannelId) : INotification;
    public record PurgeWordNotification(uint ChannelId, string Word) : INotification;
    public record ReceiveMessageNotification(ChatMessage Message) : INotification;
    public record RefreshedTokensNotification(UserAccessToken Tokens) : INotification;
    public record SendMessageNotification(uint ChannelId, string Message) : INotification;
}
