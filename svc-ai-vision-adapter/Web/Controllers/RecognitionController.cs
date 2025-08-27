using Microsoft.AspNetCore.Mvc;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;

namespace svc_ai_vision_adapter.Web.Controllers
{
    [ApiController]
    [Route("analyze")]
    public class recognitionController(IRecognitionService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<RecognitionResponseDto>> Analyze([FromBody] RecognitionRequestDto req, CancellationToken ct)
        {
            if (req.Images is null || req.Images.Count is < 1 or > 10)
                return BadRequest(new { error = "Images must contain 1..10 items" });
            
            var res = await service.AnalyzeAsync(req, ct);
            return Ok(res);
        }
        
    }
}
