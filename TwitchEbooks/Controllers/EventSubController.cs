using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TwitchEbooks.Controllers
{
    [ApiController, Route("[controller]")]
    public class EventSubController : ControllerBase
    {
        private readonly ILogger<EventSubController> _logger;

        public EventSubController(ILogger<EventSubController> logger)
        {
            _logger = logger;
        }
    }
}
