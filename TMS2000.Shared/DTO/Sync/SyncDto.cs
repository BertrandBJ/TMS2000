using System.ComponentModel.DataAnnotations;
using TMS2000.Shared;

namespace TMS2000.Shared.DTO.Sync;

public record SyncRequest(
    [Required, MinLength(1), MaxLength(100)] List<SyncAction> Actions);   // 1 à 100 actions par synchro

public record SyncAction(
    [Required, StringLength(64)] string TransactionId,   // GUID généré par le mobile à la création de l'action
    [Required] string ResourceId,
    [Required] TransactionType? Type,
    [Range(1, 1_000_000)] int Quantity,
    [Required] DateTime? CreatedAt);                     // quand le joueur a fait l'action, hors ligne

public record SyncResponse(
    List<SyncActionResult> Results,
    decimal NewBalance);

public record SyncActionResult(
    string TransactionId,
    TransactionStatus Status,       // Accepted ou Rejected
    decimal ExecutedPrice,          // le prix historique appliqué (0 si rejetée)
    string? RejectReason,
    decimal? Profit,                // vente uniquement
    decimal? Efficiency,            // vente uniquement
    decimal? OfflineBonus);         // vente gagnante uniquement