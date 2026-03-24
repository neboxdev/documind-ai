using DocuMind.Application.Features.Providers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// List available AI providers with their default model and whether each is the default.
    /// Only providers with a configured API key are returned.
    /// </summary>
    [HttpGet(Name = nameof(GetAvailable))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAvailableProvidersQuery(), ct);
        return Ok(result);
    }
}
