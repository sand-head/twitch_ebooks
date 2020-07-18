using Anybotty.StreamClientLibrary.Twitch;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Models;

namespace TwitchEbooks.Infrastructure
{
    public class AuthCodeFlowWebServerFactory
    {
        private readonly TwitchSettings _twitchSettings;
        private readonly TwitchApiClient _apiClient;

        public AuthCodeFlowWebServerFactory(TwitchSettings twitchSettings, TwitchApiClient apiClient)
        {
            _twitchSettings = twitchSettings;
            _apiClient = apiClient;
        }

        public AuthCodeFlowWebServer CreateServer()
        {
            return new AuthCodeFlowWebServer(_twitchSettings, _apiClient);
        }

        public class AuthCodeFlowWebServer : IDisposable
        {
            private readonly TwitchSettings _twitchSettings;
            private readonly TwitchApiClient _apiClient;
            private readonly WebServer _server;

            private UserAccessToken _accessToken;

            public AuthCodeFlowWebServer(TwitchSettings twitchSettings, TwitchApiClient apiClient)
            {
                _twitchSettings = twitchSettings;
                _apiClient = apiClient;

                _server = new WebServer()
                    .WithAction("/callback", HttpVerbs.Get, HandleAuthCodeFlow);
                _accessToken = default;
            }

            public async Task<UserAccessToken> RunUntilCompleteAsync(CancellationToken token = default)
            {
                _server.Start();
                while (!token.IsCancellationRequested && _accessToken == default)
                {
                    await Task.Delay(1000);
                }
                return _accessToken;
            }

            public void Dispose()
            {
                _server.Dispose();
            }

            private async Task HandleAuthCodeFlow(IHttpContext context)
            {
                var queryData = context.GetRequestQueryData();
                var code = queryData.Get("code");
                if (code == null)
                {
                    // I don't actually really care to make responses more complex than the status code right now
                    // the fact that I even gotta make a temporary web server for this console service is bad enough as it is
                    await context.SendStandardHtmlAsync(StatusCodes.Status400BadRequest);
                    return;
                }

                try
                {
                    var (authMsg, verifyMsg) = await _apiClient.AuthorizeAndVerifyAsync(code, _twitchSettings.ClientId, _twitchSettings.ClientSecret, _twitchSettings.RedirectUri);
                    _accessToken = new UserAccessToken
                    {
                        Id = Guid.NewGuid(),
                        UserId = Convert.ToUInt32(verifyMsg.UserId),
                        AccessToken = authMsg.AccessToken,
                        RefreshToken = authMsg.RefreshToken,
                        ExpiresIn = authMsg.ExpiresIn,
                        CreatedOn = DateTime.UtcNow
                    };
                }
                catch
                {
                    await context.SendStandardHtmlAsync(StatusCodes.Status500InternalServerError);
                    return;
                }

                await context.SendStandardHtmlAsync(StatusCodes.Status201Created);
            }
        }
    }
}
