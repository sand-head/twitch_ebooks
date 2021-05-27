using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Database.Models;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models;
using TwitchEbooks.Services;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace TwitchEbooks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting host...");
                var host = CreateHostBuilder(args).Build();
                MigrateDatabase(host);
                await StartWebHostIfFirstRunAsync(host);
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Database context
                    services.AddDbContext<TwitchEbooksContext>(options =>
                        options.UseNpgsql(context.Configuration.GetConnectionString("PostgreSQL")),
                        optionsLifetime: ServiceLifetime.Transient);
                    services.AddDbContextFactory<TwitchEbooksContext, TwitchEbooksContextFactory>();

                    // Configuration
                    var twitchSettings = context.Configuration.GetSection("Twitch").Get<TwitchSettings>();
                    services.AddSingleton(twitchSettings);

                    // Misc. dependencies
                    services
                        .AddMediatR(typeof(Program))
                        .AddHttpClient()
                        .AddSingleton<MessageGenerationQueue>()
                        .AddSingleton<IMarkovChainService, MarkovChainService>();

                    // TwitchLib stuff
                    services
                        .AddSingleton<TwitchAPI>(services =>
                        {
                            var context = services.CreateScope().ServiceProvider.GetRequiredService<TwitchEbooksContext>();
                            var tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

                            var api = new TwitchAPI();
                            api.Settings.AccessToken = tokens.AccessToken;
                            api.Settings.ClientId = twitchSettings.ClientId;

                            return api;
                        })
                        .AddSingleton<ConnectionCredentials>(services =>
                        {
                            var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient();
                            var context = services.CreateScope().ServiceProvider.GetRequiredService<TwitchEbooksContext>();
                            var tokens = context.AccessTokens.OrderByDescending(a => a.CreatedOn).First();

                            // manually validate tokens
                            // I cannot believe TwitchLib is only *just now* adding this endpoint
                            // for a library with so much community use how is it not actively supported
                            var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
                            request.Headers.Add("Authorization", $"OAuth {tokens.AccessToken}");
                            var response = httpClient.SendAsync(request).GetAwaiter().GetResult();

                            if (!response.IsSuccessStatusCode)
                            {
                                Log.Information("Refreshing tokens...");
                                var api = services.GetRequiredService<TwitchAPI>();
                                var settings = services.GetRequiredService<TwitchSettings>();
                                var refreshResponse = api.V5.Auth.RefreshAuthTokenAsync(tokens.RefreshToken, settings.ClientSecret).GetAwaiter().GetResult();
                                var createdOn = DateTime.UtcNow;

                                tokens = new UserAccessToken
                                {
                                    UserId = tokens.UserId,
                                    AccessToken = refreshResponse.AccessToken,
                                    RefreshToken = refreshResponse.RefreshToken,
                                    ExpiresIn = refreshResponse.ExpiresIn,
                                    CreatedOn = createdOn
                                };

                                api.Settings.AccessToken = tokens.AccessToken;
                                context.AccessTokens.Add(tokens);
                                context.SaveChanges();
                                Log.Information("Tokens refreshed!");
                            }

                            return new ConnectionCredentials(twitchSettings.BotUsername, tokens.AccessToken);
                        })
                        .AddSingleton<TwitchClient>(services =>
                        {
                            var client = new TwitchClient();
                            client.Initialize(services.GetRequiredService<ConnectionCredentials>());

                            return client;
                        });

                    // Hosted services
                    services
                        .AddHostedService<MessageGenerationService>()
                        .AddHostedService<TwitchService>();
                })
                .UseSerilog();

        static void MigrateDatabase(IHost host)
        {
            using var scope = host.Services.CreateScope();

            try
            {
                var context = scope.ServiceProvider.GetRequiredService<TwitchEbooksContext>();
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred migrating the database: {Message}", ex.Message);
            }
        }

        static async Task StartWebHostIfFirstRunAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TwitchEbooksContext>();

            if (!await context.AccessTokens.AnyAsync())
            {
                // todo: re-do this using Kestrel maybe? either way we should do it *before* creating the host I think
                // that'll solve the TwitchLib Issue
                /*
                Log.Information("No access tokens were found in the database, pulling up the web server...");
                var webServerFactory = host.Services.GetRequiredService<AuthCodeFlowWebServerFactory>();
                var twitchSettings = host.Services.GetRequiredService<TwitchSettings>();
                using var server = webServerFactory.CreateServer();

                var scopes = new string[]
                {
                    "chat:edit",
                    "chat:read"
                };

                var authCodeFlowUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={WebUtility.UrlEncode(twitchSettings.ClientId)}"
                    + $"&redirect_uri={WebUtility.UrlEncode(twitchSettings.RedirectUri)}&response_type=code&scope={WebUtility.UrlEncode(string.Join(' ', scopes))}";
                Log.Information("In order to connect to Twitch, we gotta do a fancy authentication handshake with them so that they'll give us some neat access tokens. "
                    + "To log in with Twitch, head on over to this URL:\n" + authCodeFlowUrl);

                context.AccessTokens.Add(await server.RunUntilCompleteAsync());
                await context.SaveChangesAsync();
                Log.Information("Alrighty, tokens acquired! We'll go ahead and continue starting up now, thanks.");
                */
                throw new Exception("No access tokens were found in the database.");
            }
        }
    }
}
