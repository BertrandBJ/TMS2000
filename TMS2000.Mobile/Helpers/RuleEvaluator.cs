using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Helpers;

public static class RuleEvaluator
{
    public static bool IsActive(SnapshotRuleDto rule, DateTime at) =>
        rule.StartsAt <= at && at < rule.EndsAt;

    public static bool IsConditionMet(SnapshotRuleDto rule, decimal price) => rule.Condition.Operator switch
    {
        ConditionOperator.LessThan => price < rule.Condition.Value,
        ConditionOperator.LessThanOrEqual => price <= rule.Condition.Value,
        ConditionOperator.GreaterThan => price > rule.Condition.Value,
        ConditionOperator.GreaterThanOrEqual => price >= rule.Condition.Value,
        _ => false
    };

    public static bool IsTriggered(SnapshotRuleDto rule, DateTime at, decimal price) =>
        IsActive(rule, at) && IsConditionMet(rule, price);

    public static decimal TaxRate(IEnumerable<SnapshotRuleDto> rules, DateTime at, decimal price) =>
        rules
            .Where(rule => rule.Effect.Type == EffectType.TransactionTax && rule.Effect.Value.HasValue)
            .Where(rule => IsTriggered(rule, at, price))
            .Sum(rule => rule.Effect.Value!.Value) / 100m;

    public static int? MaxQuantity(IEnumerable<SnapshotRuleDto> rules, DateTime at, decimal price)
    {
        List<int> quantityLimits = rules
            .Where(rule => rule.Effect.Type == EffectType.QuantityLimit && rule.Effect.Value.HasValue)
            .Where(rule => IsTriggered(rule, at, price))
            .Select(rule => (int)rule.Effect.Value!.Value)
            .ToList();

        return quantityLimits.Count == 0 ? null : quantityLimits.Min();
    }
}
