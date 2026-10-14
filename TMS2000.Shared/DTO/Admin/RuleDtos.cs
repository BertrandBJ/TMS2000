using System.ComponentModel.DataAnnotations;
using TMS2000.Shared;

namespace TMS2000.Shared.DTO.Admin;

public record RuleConditionDto(
    [Required] string Field,                    // seul "price" est supporté
    [Required] ConditionOperator? Operator,
    [Range(0.01, 1_000_000)] decimal Value);

public record RuleEffectDto(
    [Required] EffectType? Type,
    decimal? Value);                            // QuantityLimit : quantité max ; TransactionTax : % ; sinon ignorée

public record CreateRuleRequest(
    [Required, MinLength(1)] List<string> ResourceIds,
    [Required] RuleConditionDto Condition,
    [Required] RuleEffectDto Effect,
    DateTime? StartsAt,                         // absent = immédiatement
    [Range(1, 168)] int DurationHours);

public record CreateRuleResponse(string RuleId);

public record RuleDto(
    string RuleId,
    List<string> ResourceIds,
    RuleConditionDto Condition,
    RuleEffectDto Effect,
    DateTime StartsAt,
    DateTime EndsAt,
    string CreatedBy);

public record RulesResponse(List<RuleDto> Rules);