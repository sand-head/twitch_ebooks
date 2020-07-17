using Anybotty.StreamClientLibrary.Twitch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models;

namespace TwitchEbooks
{
    public class Program
    {
        public static void Main(string[] args)
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
                host.Run();
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

        private static void MigrateDatabase(IHost host)
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<TwitchEbooksContext>(options =>
                        options.UseNpgsql(hostContext.Configuration.GetConnectionString("PostgreSQL")));
                    services.AddHttpClient<TwitchApiClient>();

                    services.AddSingleton(hostContext.Configuration.GetSection("Twitch").Get<TwitchSettings>());
                    services.AddSingleton<TwitchService>();
                    services.AddSingleton<MessageGenerationPool>();

                    services.AddHostedService<Worker>();
                })
                .UseSerilog();
    }
}
