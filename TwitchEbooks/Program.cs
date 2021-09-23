using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Threading.Tasks;
using TwitchEbooks.Database;

namespace TwitchEbooks
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                //.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Warning,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] <{SourceContext}> {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code,
                    applyThemeToRedirectedOutput: true)
                .WriteTo.Seq(configuration.GetConnectionString("Seq"))
                .CreateLogger();
            var logger = Log.ForContext<Program>();

            try
            {
                logger.Information("Starting host...");
                var host = CreateHostBuilder(args, configuration).Build();
                MigrateDatabase(host);
                await StartWebHostIfFirstRunAsync(host);
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration config) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "TwitchEbooks")
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] <{SourceContext}> {Message:lj}{NewLine}{Exception}",
                        theme: AnsiConsoleTheme.Code,
                        applyThemeToRedirectedOutput: true)
                    .WriteTo.Seq(config.GetConnectionString("Seq")));

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
