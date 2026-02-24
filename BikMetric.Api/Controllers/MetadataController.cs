using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BikMetric.Application.Metadata.Queries.GetTables;

namespace BikMetric.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetadataController : ControllerBase
{
    private readonly IMediator _mediator;

    public MetadataController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        var result = await _mediator.Send(new GetTablesQuery());
        return Ok(result);
    }
}
