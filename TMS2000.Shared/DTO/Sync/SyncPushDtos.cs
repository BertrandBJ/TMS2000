using System.ComponentModel.DataAnnotations;
using TMS2000.Shared;

namespace TMS2000.Shared.DTO.Sync;


// sens : téléphone => serveur. push

public record SyncPushRequest(
    [Required, MinLength(1), MaxLength(100)] List<OfflineAction> Actions);   // 1 à 100 actions par synchro

public record OfflineAction(
    [Required, StringLength(64)] string TransactionId,   // GUID généré par le mobile à la création de l'action
    [Required] string ResourceId,
    [Required] TransactionType? Type,
    [Range(1, 1_000_000)] int Quantity,
    [Required] DateTime? CreatedAt);                     // quand le joueur a fait l'action, hors ligne

public record SyncPushResponse(
    List<OfflineActionResult> Results,
    decimal NewBalance,
    GameSnapshot Snapshot);

public record OfflineActionResult(
    string TransactionId,
    TransactionStatus Status,       // Accepted ou Rejected
    decimal ExecutedPrice,          // le prix historique appliqué (0 si rejetée)
    string? RejectReason,
    decimal? Profit,                // vente uniquement
    decimal? Efficiency,            // vente uniquement
    decimal? OfflineBonus);         // vente gagnante uniquement



