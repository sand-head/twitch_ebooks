using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models;
using TwitchEbooks.Models.MediatR.Requests;
using TwitchEbooks.Twitch.Api;

namespace TwitchEbooks.Handlers.Requests
{
    public class RefreshTokensRequestHandler : IRequestHandler<RefreshTokensRequest, UserAccessToken>
    {
        private readonly ILogger<RefreshTokensRequestHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly TwitchApi _twitchApi;
        private readonly TwitchSettings _settings;

        public RefreshTokensRequestHandler(
            ILogger<RefreshTokensRequestHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            TwitchApi twitchApi,
            TwitchSettings settings)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _twitchApi = twitchApi;
            _settings = settings;
        }

        public async Task<UserAccessToken> Handle(RefreshTokensRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refreshing tokens...");
            using var context = _contextFactory.CreateDbContext();
            var oldTokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();
            var refreshResponse = await _twitchApi.RefreshTokensAsync(oldTokens.RefreshToken, _settings.ClientId, _settings.ClientSecret);
            var createdOn = DateTime.UtcNow;

            var newTokens = new UserAccessToken
            {
                UserId = oldTokens.UserId,
                AccessToken = refreshResponse.AccessToken,
                RefreshToken = refreshResponse.RefreshToken,
                ExpiresIn = refreshResponse.ExpiresIn,
                CreatedOn = createdOn
            };

            // _twitchApi.Settings.AccessToken = newTokens.AccessToken;
            context.AccessTokens.Add(newTokens);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tokens refreshed!");
            return newTokens;
        }
    }
}
