using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Helpers;

public static class RuleDescriber
{
    public static string DescribeAll(List<SnapshotRuleDto> rules, DateTime serverTime, decimal currentPrice)
    {
        if (rules.Count == 0)
        {
            return "Aucune règle en cours ou à venir.";
        }

        return DescribeEach(rules, serverTime, currentPrice);
    }

    public static string DescribeEach(List<SnapshotRuleDto> rules, DateTime serverTime, decimal currentPrice)
    {
        IEnumerable<string> ruleDescriptions = rules
            .OrderBy(rule => rule.StartsAt)
            .Select(rule => Describe(rule, serverTime, currentPrice));

        return string.Join(Environment.NewLine, ruleDescriptions);
    }

    public static string Describe(SnapshotRuleDto rule, DateTime serverTime, decimal currentPrice)
    {
        string effectDescription = rule.Effect.Type switch
        {
            EffectType.BuyBlocked => "Achat bloqué",
            EffectType.SellBlocked => "Vente bloquée",
            EffectType.QuantityLimit => $"Quantité limitée à {rule.Effect.Value:0} par transaction",
            EffectType.TransactionTax => $"Taxe de {rule.Effect.Value:0.##} %",
            _ => "Effet inconnu"
        };

        string operatorSymbol = rule.Condition.Operator switch
        {
            ConditionOperator.LessThan => "<",
            ConditionOperator.LessThanOrEqual => "≤",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.GreaterThanOrEqual => "≥",
            _ => "?"
        };

        bool isActive = RuleEvaluator.IsActive(rule, serverTime);
        bool isTriggered = RuleEvaluator.IsTriggered(rule, serverTime, currentPrice);

        string periodDescription = isActive
            ? $"active jusqu'au {rule.EndsAt.ToLocalTime():dd/MM HH:mm}"
            : $"à venir : du {rule.StartsAt.ToLocalTime():dd/MM HH:mm} au {rule.EndsAt.ToLocalTime():dd/MM HH:mm}";

        string marker = isTriggered ? "⚠ EN VIGUEUR :" : "•";

        return $"{marker} {effectDescription} si prix {operatorSymbol} {rule.Condition.Value:0.##} $ ({periodDescription})";
    }
}
