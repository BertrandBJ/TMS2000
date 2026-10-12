using TMS2000.Shared;
namespace TMS2000.Core.Models;

public class Resource
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public PriceMode Mode { get; set; }
    public DateTime ModeChangedAt { get; set; }
    public List<PricePoint> PriceHistory { get; set; } = new();
    public List<MarketRule> Rules { get; set; } = new();
}

public class PricePoint
{
    public decimal Price { get; set; }
    public DateTime At { get; set; }
}

public class MarketRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString();
    public RuleCondition Condition { get; set; } = new();
    public RuleEffect Effect { get; set; } = new();
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RuleCondition
{
    public string Field { get; set; } = "price";
    public ConditionOperator Operator { get; set; }
    public decimal Value { get; set; }
}

public class RuleEffect
{
    public EffectType Type { get; set; }
    public decimal? Value { get; set; }
}