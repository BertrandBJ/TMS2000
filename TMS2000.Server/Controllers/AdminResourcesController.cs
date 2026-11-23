using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core.Services;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/admin/resources")]
[Authorize(Roles = Roles.Admin)]
public class AdminResourcesController : ControllerBase
{
    private readonly AdminResourceService _adminResourceService;

    public AdminResourcesController(AdminResourceService adminResourceService)
    {
        _adminResourceService = adminResourceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminResourcesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResources() =>
        Ok(await _adminResourceService.GetResourcesAsync());

    [HttpPut("{resourceId}/price-band")]
    [ProducesResponseType(typeof(AdminResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePriceBand(string resourceId, UpdatePriceBandRequest request) =>
        Ok(await _adminResourceService.UpdatePriceBandAsync(resourceId, request));
}
