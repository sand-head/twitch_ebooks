using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Models.Notifications;
using TwitchLib.Api;
using TwitchLib.Client;

namespace TwitchEbooks.Handlers
{
    public class SendMessageNotificationHandler : INotificationHandler<SendMessageNotification>
    {
        private readonly ILogger<SendMessageNotificationHandler> _logger;
        private readonly TwitchAPI _api;
        private readonly TwitchClient _client;
        private readonly Dictionary<uint, string> _idNameMap;

        public SendMessageNotificationHandler(ILogger<SendMessageNotificationHandler> logger, TwitchAPI api, TwitchClient client)
        {
            _logger = logger;
            _api = api;
            _client = client;
            _idNameMap = new Dictionary<uint, string>();
        }

        public async Task Handle(SendMessageNotification notification, CancellationToken cancellationToken)
        {
            var (channelId, message) = notification;
            if (!_idNameMap.TryGetValue(channelId, out var channelName))
            {
                channelName = await GetChannelNameById(channelId);
                _idNameMap.Add(channelId, channelName);
            }

            _client.SendMessage(channelName, message);
            _logger.LogInformation("Sent message to channel {Id}.", channelId);
        }

        private async Task<string> GetChannelNameById(uint channelId)
        {
            var response = await _api.Helix.Users.GetUsersAsync(ids: new List<string> { channelId.ToString() });
            if (response.Users.Length != 1)
                throw new Exception("Could not get user details by ID");
            return response.Users[0].Login;
        }
    }
}
