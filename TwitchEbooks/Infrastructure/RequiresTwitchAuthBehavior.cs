using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TwitchEbooks.Models.Attributes;
using TwitchEbooks.Models.MediatR.Requests;

namespace TwitchEbooks.Infrastructure
{
    public class RequiresTwitchAuthBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<RequiresTwitchAuthBehavior<TRequest, TResponse>> _logger;
        private readonly IMediator _mediator;

        public RequiresTwitchAuthBehavior(ILogger<RequiresTwitchAuthBehavior<TRequest, TResponse>> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _logger.LogInformation("Entered RequiresTwitchAuthBehavior");
            var requiresAuthAttribute = typeof(TRequest).GetCustomAttribute<RequiresTwitchAuthAttribute>();
            if (requiresAuthAttribute is null)
            {
                _logger.LogInformation("Handler for {Request} does not have required attribute, moving on.", typeof(TRequest).Name);
                return await next();
            }

            _logger.LogInformation("Handling {Request} request with RequiresTwitchAuthAttribute...", typeof(TRequest).Name);
            return await Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 1,
                    sleepDurationProvider: i => TimeSpan.FromMilliseconds(i * 500),
                    onRetryAsync: async (exception, timeSpan) =>
                    {
                        _logger.LogWarning(exception, "Request {Request} failed to authenticate with Twitch, retrying after {TotalSeconds} second(s)...", typeof(TRequest).Name, timeSpan.TotalSeconds);
                        await _mediator.Send(new RefreshTokensRequest());
                    })
                .ExecuteAsync(async () => await next());
        }
    }
}
