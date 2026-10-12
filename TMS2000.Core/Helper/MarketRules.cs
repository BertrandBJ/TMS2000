using TMS2000.Core.Models;
using TMS2000.Shared;

namespace TMS2000.Core.Helper;

public static class MarketRules
{
    // Une règle est active entre StartsAt (inclus) et EndsAt (exclu)
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

    // Règles actives ET dont la condition est vraie au prix donné
    public static List<MarketRule> Triggered(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        rules.Where(rule => IsActive(rule, at) && IsConditionMet(rule.Condition, price)).ToList();

    public static bool CanBuy(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        !Triggered(rules, at, price).Any(rule => rule.Effect.Type == EffectType.BuyBlocked);

    public static bool CanSell(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        !Triggered(rules, at, price).Any(rule => rule.Effect.Type == EffectType.SellBlocked);

    // Quantité max par transaction, ou null si aucune limite ne s'applique
    public static int? MaxQuantity(IEnumerable<MarketRule> rules, DateTime at, decimal price)
    {
        var limits = Triggered(rules, at, price)
            .Where(rule => rule.Effect.Type == EffectType.QuantityLimit && rule.Effect.Value.HasValue)
            .Select(rule => (int)rule.Effect.Value!.Value)
            .ToList();

        return limits.Count == 0 ? null : limits.Min();
    }

    // Taux de taxe (0.10 = 10 %), 0 si aucune taxe ne s'applique
    public static decimal TaxRate(IEnumerable<MarketRule> rules, DateTime at, decimal price) =>
        Triggered(rules, at, price)
            .Where(rule => rule.Effect.Type == EffectType.TransactionTax && rule.Effect.Value.HasValue)
            .Sum(rule => rule.Effect.Value!.Value) / 100m;

    // Deux périodes [startA, endA[ et [startB, endB[ se chevauchent
    public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB) =>
        startA < endB && startB < endA;

    // Texte lisible pour le joueur, ex : "Achat bloqué si prix < 20"
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