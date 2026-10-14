using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core;
using TMS2000.Core.Services;
using TMS2000.Server.Extensions;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/admin/rules")]
[Authorize(Roles = Roles.Admin)] // un Player reçoit 403
public class AdminRulesController : ControllerBase
{
    private readonly RuleService _ruleService;

    public AdminRulesController(RuleService ruleService)
    {
        _ruleService = ruleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RulesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRules() =>
        Ok(await _ruleService.GetActiveAndUpcomingAsync());

    [HttpPost]
    [ProducesResponseType(typeof(CreateRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateRule(CreateRuleRequest request)
    {
        var response = await _ruleService.CreateAsync(User.GetEmail(), request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpDelete("{ruleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(string ruleId)
    {
        await _ruleService.DeleteAsync(ruleId);
        return NoContent();
    }
}