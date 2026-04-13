using GenderClassifierApi.Models;
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
        if (!Request.Query.ContainsKey("name") || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new ErrorEnvelope("Missing or empty name parameter"));
        }

        if (Request.Query["name"].Count != 1)
        {
            return UnprocessableEntity(new ErrorEnvelope("name is not a string"));
        }

        var result = await _service.ClassifyAsync(name, cancellationToken);
        return StatusCode(result.StatusCode, result.Payload);
    }
}
