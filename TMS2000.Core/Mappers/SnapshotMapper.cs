using TMS2000.Core.Models;
using TMS2000.Shared.DTO.Admin;
using TMS2000.Shared.DTO.Market;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Core.Mappers;

// Transforme les modèles lus en base (User, Resource) en snapshot envoyé au mobile.
// Fonction pure : ni base de données ni horloge, tout arrive en paramètre.

public static class SnapshotMapper
{
    public static GameSnapshot ToGameSnapshot(User player, IEnumerable<Resource> resources, DateTime now, DateTime nextTickAt) => new(
        ServerTime: now,
        NextTickAt: nextTickAt,
        Player: new SnapshotPlayerDto(
            Email: player.Email,
            Balance: player.Balance,
            Portfolio: player.Portfolio.Select(PlayerMapper.ToHoldingDto).ToList(),
            Transactions: player.Transactions.Select(PlayerMapper.ToTransactionDto).ToList()),
        Resources: resources.Select(resource => ToSnapshotResourceDto(resource, now)).ToList());

    private static SnapshotResourceDto ToSnapshotResourceDto(Resource resource, DateTime now) => new(
        Id: resource.Id,
        Name: resource.Name,
        CurrentPrice: resource.CurrentPrice,
        RecentPrices: resource.PriceHistory
            .Select(pricePoint => new PricePointDto(pricePoint.Price, pricePoint.At))
            .ToList(),
        Rules: resource.Rules
            .Where(rule => rule.EndsAt > now)       // actives ET à venir ; une règle terminée ne sert plus au mobile
            .OrderBy(rule => rule.StartsAt)
            .Select(ToSnapshotRuleDto)
            .ToList());

    private static SnapshotRuleDto ToSnapshotRuleDto(MarketRule rule) => new(
        RuleId: rule.RuleId,
        Condition: new RuleConditionDto(rule.Condition.Field, rule.Condition.Operator, rule.Condition.Value),
        Effect: new RuleEffectDto(rule.Effect.Type, rule.Effect.Value),
        StartsAt: rule.StartsAt,
        EndsAt: rule.EndsAt);
}