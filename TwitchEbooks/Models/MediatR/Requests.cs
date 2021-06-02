using MediatR;
using TwitchEbooks.Database.Models;

namespace TwitchEbooks.Models.MediatR.Requests
{
    public record RefreshTokensRequest() : IRequest<UserAccessToken>;
    public record SendMessageRequest(uint ChannelId, string Message) : IRequest;
}
