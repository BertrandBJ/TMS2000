using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS2000.Core.Services;
using TMS2000.Server.Extensions;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Shared;

namespace TMS2000.Server.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize(Roles = Roles.Player)] // seul un joueur a un état de jeu et des actions hors ligne à synchroniser
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;

    public SyncController(SyncService syncService)
    {
        _syncService = syncService;
    }

    // PULL : serveur → téléphone. L'état du jeu, à recopier dans la base locale.
    [HttpGet]
    [ProducesResponseType(typeof(GameSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSnapshot() =>
        Ok(await _syncService.GetSnapshotAsync(User.GetUserId()));

    // PUSH : téléphone → serveur. Rejeu des actions hors ligne, réponse avec l'état à jour.
    [HttpPost]
    [ProducesResponseType(typeof(SyncPushResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Push(SyncPushRequest request) =>
        Ok(await _syncService.PushAsync(User.GetUserId(), request));
}