using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Core.Services;

public class SyncService
{
    private readonly UserRepository _userRepository;
    private readonly ResourceRepository _resourceRepository;

    public SyncService(UserRepository userRepository, ResourceRepository resourceRepository)
    {
        _userRepository = userRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<SyncResponse> SyncAsync(string userId, SyncRequest request)
    {
        // Les ressources concernées, chargées une seule fois avec leur historique complet

        var resourceIds = request.Actions.Select(action => action.ResourceId).Distinct().ToList();
        var resourcesById = (await _resourceRepository.GetByIdsAsync(resourceIds))
            .ToDictionary(resource => resource.Id);

        for (var attempt = 1; attempt <= GameSettings.TradeMaxAttempts; attempt++)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new NotFoundException("Joueur introuvable.");
            var syncedAt = DateTime.UtcNow;

            // Rejeu dans l'ordre où le joueur a fait les actions

            var results = new List<SyncActionResult>();
            foreach (var action in request.Actions.OrderBy(action => action.CreatedAt))
                results.Add(ReplayAction(user, resourcesById, action, syncedAt));

            if (user.Transactions.Count > GameSettings.MaxTransactionsPerUser)
                user.Transactions.RemoveRange(0, user.Transactions.Count - GameSettings.MaxTransactionsPerUser);

            // Toute la synchro est écrite en une fois, sous contrôle de version
            var expectedVersion = user.Version;
            user.Version++;

            if (await _userRepository.ReplaceIfVersionMatchesAsync(user, expectedVersion))
                return new SyncResponse(results, user.Balance);
        }

        throw new ConflictException("Le compte a été modifié par une autre opération pendant la synchronisation. Réessayez.");
    }

    private static SyncActionResult ReplayAction(User user, Dictionary<string, Resource> resourcesById, SyncAction action, DateTime syncedAt)
    {
        // Idempotence : action déjà traitée (acceptée OU rejetée) → même résultat

        var alreadyProcessed = user.Transactions.FirstOrDefault(t => t.TransactionId == action.TransactionId);
        if (alreadyProcessed != null)
            return ToResult(alreadyProcessed);

        var type = action.Type!.Value;
        var createdAt = ToUtc(action.CreatedAt!.Value);
        resourcesById.TryGetValue(action.ResourceId, out var resource);

        try
        {
            EnsureActionDateIsValid(createdAt, syncedAt);

            if (resource == null)
                throw new BusinessRuleException($"Ressource '{action.ResourceId}' introuvable.");

            var historicalPrice = PriceAt(resource, createdAt)
                                  ?? throw new BusinessRuleException("Aucun prix connu pour cette ressource à la date de l'action.");

            // Même moteur que le trade en ligne, mais au prix et aux règles de createdAt
            var transaction = TradeEngine.Execute(user, resource, action.TransactionId, type,
                action.Quantity, historicalPrice, createdAt, offline: true);

            transaction.ExecutedAt = syncedAt;   // décidée à createdAt, exécutée maintenant
            ApplyOfflineBonus(user, transaction, createdAt, syncedAt);

            user.Transactions.Add(transaction);
            return ToResult(transaction);
        }
        catch (BusinessRuleException rejection)
        {
            // Refus métier : enregistré, pour qu'une synchro relancée renvoie le même refus

            var rejectedTransaction = new Transaction
            {
                TransactionId = action.TransactionId,
                ResourceId = action.ResourceId,
                ResourceName = resource?.Name ?? action.ResourceId,
                Type = type,
                Quantity = action.Quantity,
                ExecutedPrice = 0m,
                CreatedAt = createdAt,
                ExecutedAt = null,               // jamais exécutée
                Offline = true,
                Status = TransactionStatus.Rejected,
                RejectReason = rejection.Message
            };

            user.Transactions.Add(rejectedTransaction);
            return ToResult(rejectedTransaction);
        }
    }

    private static void EnsureActionDateIsValid(DateTime createdAt, DateTime syncedAt)
    {
        if (createdAt > syncedAt.AddMinutes(GameSettings.ClockSkewToleranceMinutes))
            throw new BusinessRuleException("Date de l'action dans le futur.");

        if (createdAt < syncedAt.AddDays(-GameSettings.MaxOfflineDays))
            throw new BusinessRuleException($"Action trop ancienne : plus de {GameSettings.MaxOfflineDays} jours hors ligne.");
    }

    // Le prix en vigueur à une date : le dernier point d'historique à cette date ou avant
    private static decimal? PriceAt(Resource resource, DateTime at) =>
        resource.PriceHistory
            .Where(point => point.At <= at)
            .OrderByDescending(point => point.At)
            .Select(point => (decimal?)point.Price)
            .FirstOrDefault();

    // Seules les ventes gagnantes reçoivent un bonus : les pertes ne sont jamais amplifiées

    private static void ApplyOfflineBonus(User user, Transaction transaction, DateTime createdAt, DateTime syncedAt)
    {
        if (transaction.Type != TransactionType.Sell || transaction.Profit is null || transaction.Profit <= 0)
            return;

        var hoursOffline = (decimal)(syncedAt - createdAt).TotalHours;
        var bonusPercent = Math.Min(hoursOffline * GameSettings.OfflineBonusPercentPerHour, GameSettings.OfflineBonusMaxPercent);
        var bonusAmount = Math.Round(transaction.Profit.Value * bonusPercent / 100m, 2);

        if (bonusAmount <= 0)
            return;

        user.Balance += bonusAmount;
        transaction.OfflineBonus = bonusAmount;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static SyncActionResult ToResult(Transaction transaction) => new(
        TransactionId: transaction.TransactionId,
        Status: transaction.Status,
        ExecutedPrice: transaction.ExecutedPrice,
        RejectReason: transaction.RejectReason,
        Profit: transaction.Profit,
        Efficiency: transaction.Efficiency,
        OfflineBonus: transaction.OfflineBonus);
}