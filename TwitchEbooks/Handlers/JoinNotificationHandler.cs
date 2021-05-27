using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models.Notifications;
using TwitchLib.Api;
using TwitchLib.Client;

namespace TwitchEbooks.Handlers
{
    public class JoinNotificationHandler : INotificationHandler<JoinNotification>
    {
        private readonly ILogger<JoinNotificationHandler> _logger;
        private readonly IDbContextFactory<TwitchEbooksContext> _contextFactory;
        private readonly IMarkovChainService _chainService;
        private readonly TwitchAPI _twitchApi;
        private readonly TwitchClient _twitchClient;

        public JoinNotificationHandler(
            ILogger<JoinNotificationHandler> logger,
            IDbContextFactory<TwitchEbooksContext> contextFactory,
            IMarkovChainService chainService,
            TwitchAPI twitchApi,
            TwitchClient twitchClient)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _chainService = chainService;
            _twitchApi = twitchApi;
            _twitchClient = twitchClient;
        }

        public async Task Handle(JoinNotification notification, CancellationToken cancellationToken)
        {
            var context = _contextFactory.CreateDbContext();
            if (context.Channels.Any(c => c.Id == notification.ChannelId))
                return;

            // make sure a user exists on Twitch first
            var usersResponse = await _twitchApi.Helix.Users.GetUsersAsync(ids: new List<string> { notification.ChannelId.ToString() });
            if (usersResponse.Users.Length != 1)
                throw new Exception("No Twitch user could be found by that ID");
            var user = usersResponse.Users[0];

            // add channel to database
            context.Channels.Add(new TwitchChannel
            {
                Id = notification.ChannelId
            });
            await context.SaveChangesAsync(cancellationToken);

            // connect to channel on Twitch client
            _twitchClient.JoinChannel(user.Login);
            // also create a Markov chain
            await _chainService.AddOrUpdateChainForChannel(notification.ChannelId);
        }
    }
}
