using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;

namespace TwitchEbooks
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IServiceProvider services)
        {
            _logger = logger;
            Services = services;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var twitchService = Services.GetService<TwitchService>();
            var msgGenService = Services.GetService<MessageGenerationService>();

            var tokens = GetLatestTokens();
            if (tokens == null)
            {
                _logger.LogInformation("No access tokens were found in the database, pulling up the web server...");
                // todo: load web server and do authy things
            }

            await twitchService.ConnectAsync(tokens);
            // todo: hook the TwitchService up with the MessageGenerationService

            // todo: execute until stop
        }

        private UserAccessToken GetLatestTokens()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<TwitchEbooksContext>();
            return context.AccessTokens.OrderByDescending(a => a.CreatedOn).FirstOrDefault();
        }
    }
}
