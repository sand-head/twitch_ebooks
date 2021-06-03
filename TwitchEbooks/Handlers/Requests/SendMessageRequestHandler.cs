using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.Attributes;
using TwitchEbooks.Models.MediatR.Requests;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers.Requests
{
    [RequiresTwitchAuth]
    public class SendMessageRequestHandler : IRequestHandler<SendMessageRequest>
    {
        private readonly ILogger<SendMessageRequestHandler> _logger;
        private readonly ITwitchUserService _userService;
        private readonly TwitchClient _client;

        public SendMessageRequestHandler(
            ILogger<SendMessageRequestHandler> logger,
            ITwitchUserService userService,
            TwitchClient client)
        {
            _logger = logger;
            _userService = userService;
            _client = client;
        }

        public async Task<Unit> Handle(SendMessageRequest request, CancellationToken cancellationToken)
        {
            var (channelId, message) = request;
            string channelName;
            try
            {
                channelName = await _userService.GetUsernameById(channelId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Could not obtain channel name from API, response did not indicate success");
                throw;
            }

            await _client.SendChatMessageAsync(channelName, message, cancellationToken);
            _logger.LogInformation("Sent message to channel {Id}.", channelId);
            return Unit.Value;
        }
    }
}
