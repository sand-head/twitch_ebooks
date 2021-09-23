using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchEbooks.Models.EventSub;

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

        [HttpPost]
        public async Task<IActionResult> ReceiveEventSubPayload([FromHeader(Name = "Twitch-Eventsub-Message-Type")] string messageType)
        {
            using var streamReader = new StreamReader(Request.Body);
            var body = await streamReader.ReadToEndAsync();
            if (!VerifyEventSubSignature(Request.Headers, body))
                return StatusCode(403);

            _logger.LogInformation("Received EventSub payload of type {PayloadType} from Twitch.", messageType);
            using var jsonDoc = JsonDocument.Parse(body);
            if (messageType == "webhook_callback_verification")
                return Ok(jsonDoc.RootElement.GetProperty("challenge").GetString());

            // put notification on queue for processing
            var payload = JsonSerializer.Deserialize<EventSubPayload>(body);
            //await twitchService.EnqueueNotificationAsync(payload);

            return Accepted();
        }

        private bool VerifyEventSubSignature(IHeaderDictionary headers, string body)
        {
            var id = headers["Twitch-Eventsub-Message-Id"];
            var timestamp = headers["Twitch-Eventsub-Message-Timestamp"];

            var encoding = new UTF8Encoding();
            var textBytes = encoding.GetBytes(id + timestamp + body);
            //var secretBytes = encoding.GetBytes(_transport.Secret);
            var secretBytes = Array.Empty<byte>();

            using var hash = new HMACSHA256(secretBytes);
            var hashBytes = hash.ComputeHash(textBytes);
            var calculatedHash = "sha256=" + BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            var signature = headers["Twitch-Eventsub-Message-Signature"];

            return calculatedHash == signature;
        }
    }
}
