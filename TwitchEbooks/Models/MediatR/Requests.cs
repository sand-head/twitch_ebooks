using MediatR;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models.Attributes;

namespace TwitchEbooks.Models.MediatR.Requests
{
    [RequiresTwitchAuth]
    public record JoinRequest(uint ChannelId) : IRequest;
    [RequiresTwitchAuth]
    public record LeaveRequest(uint ChannelId) : IRequest;
    public record RefreshTokensRequest() : IRequest<UserAccessToken>;
    [RequiresTwitchAuth]
    public record SendMessageRequest(uint ChannelId, string Message) : IRequest;
}
