using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core;
using TMS2000.Core.Services;
using TMS2000.Server.Extensions;
using TMS2000.Shared.DTO.Trades;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/trades")]
[Authorize(Roles = Roles.Player)] // un Admin reçoit 403
public class TradesController : ControllerBase
{
    private readonly TradeService _tradeService;

    public TradesController(TradeService tradeService)
    {
        _tradeService = tradeService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Trade(TradeRequest request) =>
        Ok(await _tradeService.ExecuteAsync(User.GetUserId(), request));
}