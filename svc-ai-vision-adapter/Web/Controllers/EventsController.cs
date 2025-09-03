using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json; 
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;

namespace svc_ai_vision_adapter.Web.Controllers
{
    /// <summary>
    /// Receives event envelopes over HTTP.
    /// When the event type is ai.vision.request, it runs the analysis and,
    /// if a ReplyTo URL is present, posts a result event back to that URL.
    /// </summary>
    [ApiController]                 
    [Route("events")]               
    public class EventsController : ControllerBase
    {
        private readonly IRecognitionService _recognition;  
        private readonly IHttpClientFactory _http;
        private readonly ILogger<EventsController> _log;

        // Constructor injection: DI supplies concrete implementations at runtime
        public EventsController(
            IRecognitionService recognition,
            IHttpClientFactory http,
            ILogger<EventsController> log)
        {
            _recognition = recognition;
            _http = http;
            _log = log;
        }

        /// <summary>
        /// Accepts a generic event envelope that contains a RecognitionRequestDto payload.
        /// </summary>
        [HttpPost] // POST /events
        public async Task<IActionResult> Receive([FromBody] EventEnvelope<RecognitionRequestDto> evt, CancellationToken ct)
        {
            if (!string.Equals(evt.Type, EventTypes.VisionRequest, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = $"Unsupported event type '{evt.Type}'", expected = EventTypes.VisionRequest });
            }

            //Verify HMAC signature/header here before processing?

            var response = await _recognition.AnalyzeAsync(evt.Data, ct); //calls the inbound port in Application layer 

            if (!string.IsNullOrWhiteSpace(evt.ReplyTo))
            {
                try
                {
                    var client = _http.CreateClient();

                    // Wrap the response in an outgoing result event
                    var resultEvent = new EventEnvelope<RecognitionResponseDto>(
                        Type: EventTypes.VisionResult,
                        Id: Guid.NewGuid().ToString("n"),
                        ReplyTo: null,
                        Data: response
                    );

                    var postResp = await client.PostAsJsonAsync(evt.ReplyTo, resultEvent, ct);
                    postResp.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    _log.LogError(ex,
                        "Failed to POST result to replyTo={ReplyTo}, sessionId={SessionId}",
                        evt.ReplyTo, response.SessionId);
                }
            }

            return Ok(new { received = evt.Id, status = "processed" });
        }
    }
}
