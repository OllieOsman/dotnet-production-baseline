using DotnetProductionBaseline.Api.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotnetProductionBaseline.Api.Controllers
{
    [ApiController]
    [Route("demo/outbound")]
    public sealed class OutboundDemoController : ControllerBase
    {
        [HttpGet("ip")]
        public async Task<IActionResult> GetIp([FromServices] ISampleExternalApi api, CancellationToken ct)
        {
            var result = await api.GetIpAsync(ct);
            return Ok(new { result });
        }
    }
}
