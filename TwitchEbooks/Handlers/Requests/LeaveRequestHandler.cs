using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Requests;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers.Requests
{
    public class LeaveRequestHandler : IRequestHandler<LeaveRequest>
    {
        private readonly ILogger<LeaveRequestHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly ITwitchUserService _userService;
        private readonly TwitchClient _twitchClient;

        public LeaveRequestHandler(
            ILogger<LeaveRequestHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService,
            ITwitchUserService userService,
            TwitchClient twitchClient)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
            _userService = userService;
            _twitchClient = twitchClient;
        }

        public async Task<Unit> Handle(LeaveRequest request, CancellationToken cancellationToken)
        {
            var channelId = request.ChannelId;
            var context = _contextFactory.CreateDbContext();
            var channel = context.Channels.FirstOrDefault(c => c.Id == channelId);
            if (channel is null)
                return Unit.Value;

            // disconnect from channel on Twitch
            // make sure a user exists on Twitch first
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

            // leave channel on Twitch client
            await _twitchClient.LeaveChannelAsync(channelName);

            // remove channel from database
            context.Channels.Remove(channel);
            await context.SaveChangesAsync(cancellationToken);

            // also remove their Markov chain
            await _chainService.RemoveChainForChannelAsync(channelId, cancellationToken);
            return Unit.Value;
        }
    }
}
