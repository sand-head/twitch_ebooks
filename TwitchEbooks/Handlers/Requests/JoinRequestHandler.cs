using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Requests;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks.Handlers.Requests
{
    public class JoinRequestHandler : IRequestHandler<JoinRequest>
    {
        private readonly ILogger<JoinRequestHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly ITwitchUserService _userService;
        private readonly TwitchClient _twitchClient;

        public JoinRequestHandler(
            ILogger<JoinRequestHandler> logger,
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

        public async Task<Unit> Handle(JoinRequest request, CancellationToken cancellationToken)
        {
            var channelId = request.ChannelId;
            var context = _contextFactory.CreateDbContext();
            if (context.Channels.Any(c => c.Id == channelId))
                return Unit.Value;

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

            // add channel to database
            context.Channels.Add(new TwitchChannel
            {
                Id = channelId
            });
            await context.SaveChangesAsync(cancellationToken);

            // connect to channel on Twitch client
            await _twitchClient.JoinChannelAsync(channelName, cancellationToken);
            // also create a Markov chain
            await _chainService.AddChainForChannelAsync(channelId, cancellationToken);
            return Unit.Value;
        }
    }
}
