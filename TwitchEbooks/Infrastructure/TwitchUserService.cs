using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Models;
using TwitchEbooks.Twitch.Api;
using TwitchEbooks.Twitch.Api.Responses;

namespace TwitchEbooks.Infrastructure
{
    public interface ITwitchUserService
    {
        Task<string> GetUsernameById(uint userId);
        Task<uint> GetIdByUsername(string username);
    }

    public class TwitchUserService : ITwitchUserService
    {
        private readonly IMemoryCache _cache;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly TwitchApiFactory _apiFactory;
        private readonly TwitchSettings _settings;

        public TwitchUserService(IMemoryCache cache, IDbContextFactory<TwitchEbooksContext> contextFactory, TwitchApiFactory apiFactory, TwitchSettings settings)
        {
            _cache = cache;
            _contextFactory = contextFactory;
            _apiFactory = apiFactory;
            _settings = settings;
        }

        public async Task<uint> GetIdByUsername(string username)
        {
            if (!_cache.TryGetValue($"_LOGIN_{username}", out uint userId))
            {
                var response = await GetUsersAsync(logins: new List<string> { username });
                userId = uint.Parse(response.Users[0].Id);

                _cache.Set($"_LOGIN_{username}", userId, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(6)));
            }

            return userId;
        }

        public async Task<string> GetUsernameById(uint userId)
        {
            if (!_cache.TryGetValue($"_ID_{userId}", out string username))
            {
                var response = await GetUsersAsync(ids: new List<string> { userId.ToString() });
                username = response.Users[0].Login;

                _cache.Set($"_ID_{userId}", username, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(6)));
            }

            return username;
        }

        private async Task<UsersResponse> GetUsersAsync(List<string> ids = null, List<string> logins = null)
        {
            var context = _contextFactory.CreateDbContext();
            var tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

            var api = _apiFactory.CreateApiClient();
            var response = await api.GetUsersAsync(tokens.AccessToken, _settings.ClientId, ids, logins);
            if (response.Users.Length == 0)
                throw new Exception("Could not get user details (user likely does not exist)");
            return response;
        }
    }
}
