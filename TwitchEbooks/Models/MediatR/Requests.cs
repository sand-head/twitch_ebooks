using MediatR;
using System;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models.Attributes;

namespace TwitchEbooks.Models.MediatR.Requests
{
    // things that just require twitch auth:
    [RequiresTwitchAuth]
    public record BanUserRequest(string Channel, string Username) : IRequest;
    [RequiresTwitchAuth]
    public record JoinRequest(uint ChannelId) : IRequest;
    [RequiresTwitchAuth]
    public record LeaveRequest(uint ChannelId) : IRequest;
    [RequiresTwitchAuth]
    public record SendMessageRequest(uint ChannelId, string Message, Guid? InReplyTo = null) : IRequest;

    // things that are actually requests:
    public record RefreshTokensRequest() : IRequest<UserAccessToken>;
}
