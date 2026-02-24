using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BikMetric.Application.Query.Commands.ExecuteQuery;

namespace BikMetric.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QueryController : ControllerBase
{
    private readonly IMediator _mediator;

    public QueryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ExecuteQueryCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
