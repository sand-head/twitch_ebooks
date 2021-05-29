using Microsoft.Extensions.DependencyInjection;
using System;

namespace TwitchEbooks.Twitch.Api
{
    public class TwitchApiFactory
    {
        private readonly IServiceProvider _services;

        public TwitchApiFactory(IServiceProvider services)
        {
            _services = services;
        }

        public TwitchApi CreateApiClient()
        {
            return _services.CreateScope().ServiceProvider.GetRequiredService<TwitchApi>();
        }
    }
}
