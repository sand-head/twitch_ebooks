using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.MediatR.Requests;

namespace TwitchEbooks.Handlers.Requests
{
    public class BanUserRequestHandler : IRequestHandler<BanUserRequest>
    {
        private readonly ILogger<BanUserRequestHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly ITwitchUserService _userService;

        public BanUserRequestHandler(
            ILogger<BanUserRequestHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService,
            ITwitchUserService userService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
            _userService = userService;
        }

        public async Task<Unit> Handle(BanUserRequest request, CancellationToken cancellationToken)
        {
            var (channelName, username) = request;
            var channelId = await _userService.GetIdByUsername(channelName);
            var userId = await _userService.GetIdByUsername(username);
            var context = _contextFactory.CreateDbContext();

            // remove all messages from that user in that channel from database
            var messages = context.Messages.Where(m => m.ChannelId == channelId && m.UserId == userId);
            context.Messages.RemoveRange(messages);
            await context.SaveChangesAsync(cancellationToken);

            // also refresh that channel's Markov chain
            await _chainService.AddChainForChannelAsync(channelId, cancellationToken);

            return Unit.Value;
        }
    }
}
