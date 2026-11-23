using TMS2000.Shared;
using TMS2000.Shared.DTO.Admin;
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
        bool isTriggered = RuleEvaluator.IsTriggered(rule, serverTime, currentPrice);
        string marker = isTriggered ? "⚠ EN VIGUEUR :" : "•";

        return $"{marker} {DescribeEffect(rule.Effect)} {DescribeCondition(rule.Condition)} "
               + $"({DescribePeriod(rule.StartsAt, rule.EndsAt, serverTime)})";
    }

    public static string DescribeEffect(RuleEffectDto effect) => effect.Type switch
    {
        EffectType.BuyBlocked => "Achat bloqué",
        EffectType.SellBlocked => "Vente bloquée",
        EffectType.QuantityLimit => $"Quantité limitée à {effect.Value:0} par transaction",
        EffectType.TransactionTax => $"Taxe de {effect.Value:0.##} %",
        _ => "Effet inconnu"
    };

    public static string DescribeCondition(RuleConditionDto condition)
    {
        string operatorSymbol = condition.Operator switch
        {
            ConditionOperator.LessThan => "<",
            ConditionOperator.LessThanOrEqual => "≤",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.GreaterThanOrEqual => "≥",
            _ => "?"
        };

        return $"si prix {operatorSymbol} {AmountFormatter.FormatMoney(condition.Value)}";
    }

    public static string DescribePeriod(DateTime startsAt, DateTime endsAt, DateTime now) =>
        startsAt <= now
            ? $"active jusqu'au {endsAt.ToLocalTime():dd/MM HH:mm}"
            : $"à venir : du {startsAt.ToLocalTime():dd/MM HH:mm} au {endsAt.ToLocalTime():dd/MM HH:mm}";
}
