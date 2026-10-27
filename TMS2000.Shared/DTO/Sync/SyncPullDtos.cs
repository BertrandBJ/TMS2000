using TMS2000.Shared.DTO.Admin;
using TMS2000.Shared.DTO.Market;
using TMS2000.Shared.DTO.Players;

namespace TMS2000.Shared.DTO.Sync;

// SENS : serveur → téléphone (pull).
// L'état du jeu vu par le joueur connecté : ce que le téléphone recopie dans sa base SQLite.
// Renvoyé par GET /api/sync (démarrage, reconnexion) et inclus dans SyncPushResponse (après le rejeu).


public record GameSnapshot(
    DateTime ServerTime,                 // heure du serveur : permet au mobile de repérer une horloge de téléphone décalée
    DateTime NextTickAt,                 // prochain changement de prix : le mobile rafraîchit le marché après cette date
    SnapshotPlayerDto Player,
    List<SnapshotResourceDto> Resources);

public record SnapshotPlayerDto(
    string Email,
    decimal Balance,
    List<HoldingDto> Portfolio,          // avec le détail des lots : nécessaire pour savoir ce qu'on peut vendre hors ligne
    List<TransactionDto> Transactions);  // tout l'historique conservé par le serveur, du plus ancien au plus récent

public record SnapshotResourceDto(
    string Id,
    string Name,
    decimal CurrentPrice,
    List<PricePointDto> RecentPrices,    // les derniers points seulement, pour afficher une courbe hors ligne
    List<SnapshotRuleDto> Rules);        // règles actives ET à venir, avec leur condition et leur effet

public record SnapshotRuleDto(
    string RuleId,
    RuleConditionDto Condition,
    RuleEffectDto Effect,
    DateTime StartsAt,
    DateTime EndsAt);