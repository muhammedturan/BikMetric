using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BikMetric.Application.Ai.Commands.AiQuery;
using BikMetric.Application.Ai.Commands.GenerateRules;

namespace BikMetric.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] AiQueryCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("generate-rules")]
    public async Task<IActionResult> GenerateRules([FromBody] GenerateRulesCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
