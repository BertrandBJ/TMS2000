using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core.Services;
using TMS2000.Shared.DTO.Market;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/market")]
[Authorize] // Player et Admin : il suffit d'être connecté
public class MarketController : ControllerBase
{
    private readonly MarketService _marketService;

    public MarketController(MarketService marketService)
    {
        _marketService = marketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MarketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMarket() =>
        Ok(await _marketService.GetMarketAsync());

    [HttpGet("{resourceId}")]
    [ProducesResponseType(typeof(MarketResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResource(string resourceId) =>
        Ok(await _marketService.GetResourceAsync(resourceId));

    [HttpGet("{resourceId}/history")]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(string resourceId, [FromQuery, Range(1, 168)] int hours = 24) =>
        Ok(await _marketService.GetHistoryAsync(resourceId, hours));
}