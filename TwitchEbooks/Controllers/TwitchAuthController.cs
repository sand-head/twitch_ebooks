using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TwitchEbooks.Database;
using TwitchEbooks.Models;

namespace TwitchEbooks.Controllers;

[ApiController, Route("api/[controller]")]
public class TwitchAuthController : ControllerBase
{
    private readonly TwitchEbooksContext _context;
    private readonly TwitchSettings _settings;
    private readonly ILogger<TwitchAuthController> _logger;

    public TwitchAuthController(TwitchEbooksContext context, TwitchSettings settings, ILogger<TwitchAuthController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task Get()
    {
        var scopes = new string[]
        {
            "chat:edit",
            "chat:read"
        };

        //_context.AccessTokens.Add(await server.RunUntilCompleteAsync());
        await _context.SaveChangesAsync();
        _logger.LogInformation("Alrighty, tokens acquired! We'll go ahead and continue starting up now, thanks.");
    }
}
