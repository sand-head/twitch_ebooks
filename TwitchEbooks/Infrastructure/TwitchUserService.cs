using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Models;
using TwitchEbooks.Twitch.Api;

namespace TwitchEbooks.Infrastructure
{
    public interface ITwitchUserService
    {
        Task<string> GetUsernameById(uint userId);
        Task<uint> GetIdByUsername(string username);
    }

    public class TwitchUserService : ITwitchUserService
    {
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly TwitchApiFactory _apiFactory;
        private readonly TwitchSettings _settings;
        // todo: don't just store these indefinitely in a dictionary
        // make like some actual caching solution so we can handle name changes
        private readonly Dictionary<uint, string> _idUsernameCache;
        private readonly Dictionary<string, uint> _usernameIdCache;

        public TwitchUserService(IDbContextFactory<TwitchEbooksContext> contextFactory, TwitchApiFactory apiFactory, TwitchSettings settings)
        {
            _contextFactory = contextFactory;
            _apiFactory = apiFactory;
            _settings = settings;
            _idUsernameCache = new Dictionary<uint, string>();
            _usernameIdCache = new Dictionary<string, uint>();
        }

        public async Task<uint> GetIdByUsername(string username)
        {
            if (_usernameIdCache.ContainsKey(username))
                return _usernameIdCache[username];

            var context = _contextFactory.CreateDbContext();
            var tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

            var api = _apiFactory.CreateApiClient();
            var response = await api.GetUsersAsync(tokens.AccessToken, _settings.ClientId, logins: new List<string> { username.ToString() });
            if (response.Users.Length != 1)
                throw new Exception("Could not get user details by username");
            return uint.Parse(response.Users[0].Id);
        }

        public async Task<string> GetUsernameById(uint userId)
        {
            if (_idUsernameCache.ContainsKey(userId))
                return _idUsernameCache[userId];

            var context = _contextFactory.CreateDbContext();
            var tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

            var api = _apiFactory.CreateApiClient();
            var response = await api.GetUsersAsync(tokens.AccessToken, _settings.ClientId, ids: new List<string> { userId.ToString() });
            if (response.Users.Length != 1)
                throw new Exception("Could not get user details by ID");
            return response.Users[0].Login;
        }
    }
}
