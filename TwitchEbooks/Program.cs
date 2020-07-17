using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;

namespace TwitchEbooks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<TwitchEbooksContext>(options =>
                        options.UseNpgsql(hostContext.Configuration.GetConnectionString("PostgreSQL")));
                    services.AddSingleton<TwitchService>();
                    services.AddSingleton<MessageGenerationPool>();
                    services.AddHostedService<Worker>();
                });
    }
}
