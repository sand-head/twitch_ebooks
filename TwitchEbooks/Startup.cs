using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchEbooks.Database;
using TwitchEbooks.Infrastructure;
using TwitchEbooks.Models;
using TwitchEbooks.Services;
using TwitchEbooks.Twitch.Api;
using TwitchEbooks.Twitch.Chat;

namespace TwitchEbooks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Database context
            services.AddDbContext<TwitchEbooksContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("PostgreSQL")),
                optionsLifetime: ServiceLifetime.Transient);
            services.AddDbContextFactory<TwitchEbooksContext, TwitchEbooksContextFactory>();

            // Configuration
            var twitchSettings = Configuration.GetSection("Twitch").Get<TwitchSettings>();
            services.AddSingleton(twitchSettings);

            // Misc. dependencies
            services.AddMemoryCache();
            services.AddHttpClient<TwitchApi>();
            services
                .AddSingleton<TwitchApiFactory>()
                .AddSingleton<TwitchClient>()
                .AddSingleton<ITwitchUserService, TwitchUserService>()
                .AddMediatR(typeof(Program))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(RequiresTwitchAuthBehavior<,>))
                .AddSingleton<MessageGenerationQueue>()
                .AddSingleton<IMarkovChainService, MarkovChainService>();

            // Hosted services
            services
                .AddHostedService<MessageGenerationService>()
                .AddHostedService<TwitchService>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
