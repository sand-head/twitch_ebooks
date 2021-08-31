using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Requests;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers.Requests
{
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
            var (channelId, message, messageId) = request;
            string channelName;
            try
            {
                channelName = await _userService.GetUsernameById(channelId);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("Could not obtain channel name from API, need to refresh tokens first");
                throw;
            }

            await _client.SendChatMessageAsync(channelName, message, messageId, cancellationToken);
            _logger.LogInformation("Sent message to channel {Id}.", channelId);
            return Unit.Value;
        }
    }
}
