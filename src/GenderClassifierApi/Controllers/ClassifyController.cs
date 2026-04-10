using GenderClassifierApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenderClassifierApi.Controllers;

[ApiController]
[Route("api")]
public sealed class ClassifyController : ControllerBase
{
    private readonly IGenderizeService _service;

    public ClassifyController(IGenderizeService service)
    {
        _service = service;
    }

    [HttpGet("classify")]
    public async Task<IActionResult> Classify([FromQuery] string? name, CancellationToken cancellationToken)
    {
        if (!Request.Query.TryGetValue("name", out var values))
        {
            return BadRequest(new { status = "error", message = "Missing or empty name parameter" });
        }

        if (values.Count != 1)
        {
            return UnprocessableEntity(new { status = "error", message = "name is not a string" });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { status = "error", message = "Missing or empty name parameter" });
        }

        var result = await _service.ClassifyAsync(name.Trim(), cancellationToken);
        return StatusCode(result.StatusCode, result.Payload);
    }
}
