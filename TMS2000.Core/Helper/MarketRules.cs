using TMS2000.Core.Models;
using TMS2000.Shared;
namespace TMS2000.Core.Helper;



public static class MarketRules
{
    
    public static bool IsActive(MarketRule rule, DateTime at) =>
        rule.StartsAt <= at && at < rule.EndsAt;

    public static bool IsConditionMet(RuleCondition condition, decimal price) => condition.Operator switch
    {
        ConditionOperator.LessThan => price < condition.Value,
        ConditionOperator.LessThanOrEqual => price <= condition.Value,
        ConditionOperator.GreaterThan => price > condition.Value,
        ConditionOperator.GreaterThanOrEqual => price >= condition.Value,
        _ => false
    };

    
    public static List<MarketRule> Triggered(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        rules.Where(rule => IsActive(rule, at) && IsConditionMet(rule.Condition, price)).ToList();

    public static bool CanBuy(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        !Triggered(rules, at, price).Any(rule => rule.Effect.Type == EffectType.BuyBlocked);

    public static bool CanSell(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        !Triggered(rules, at, price).Any(rule => rule.Effect.Type == EffectType.SellBlocked);

    
    public static int? MaxQuantity(IEnumerable<MarketRule> rules, DateTime at, decimal price)
    {
        var limits = Triggered(rules, at, price)
            .Where(rule => rule.Effect.Type == EffectType.QuantityLimit && rule.Effect.Value.HasValue)
            .Select(rule => (int)rule.Effect.Value!.Value)
            .ToList();

        return limits.Count == 0 ? null : limits.Min();
    }

    
    public static decimal TaxRate(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        Triggered(rules, at, price)
            .Where(rule => rule.Effect.Type == EffectType.TransactionTax && rule.Effect.Value.HasValue)
            .Sum(rule => rule.Effect.Value!.Value) / 100m;

    
    public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB) =>
        startA < endB && startB < endA;

    
    public static string Describe(MarketRule rule)
    {
        var effect = rule.Effect.Type switch
        {
            EffectType.BuyBlocked => "Achat bloqué",
            EffectType.SellBlocked => "Vente bloquée",
            EffectType.QuantityLimit => $"Quantité limitée à {rule.Effect.Value:0} par transaction",
            EffectType.TransactionTax => $"Taxe de {rule.Effect.Value:0.##} %",
            _ => "Effet inconnu"
        };

        var symbol = rule.Condition.Operator switch
        {
            ConditionOperator.LessThan => "<",
            ConditionOperator.LessThanOrEqual => "≤",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.GreaterThanOrEqual => "≥",
            _ => "?"
        };

        return $"{effect} si prix {symbol} {rule.Condition.Value:0.##}";
    }
}