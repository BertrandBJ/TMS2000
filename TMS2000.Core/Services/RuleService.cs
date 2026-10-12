using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Admin;
using TMS2000.Shared;

namespace TMS2000.Core.Services;

public class RuleService
{
    // Tolérance pour une date de début "maintenant" envoyée avec un léger décalage d'horloge
    private static readonly TimeSpan StartTolerance = TimeSpan.FromMinutes(5);

    private readonly ResourceRepository _resourceRepository;

    public RuleService(ResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<RulesResponse> GetActiveAndUpcomingAsync()
    {
        var now = DateTime.UtcNow;
        var resources = await _resourceRepository.GetAllWithoutHistoryAsync();

        // Une même règle est copiée dans chaque ressource ciblée : on regroupe par RuleId
        var rules = resources
            .SelectMany(resource => resource.Rules.Select(rule => (ResourceId: resource.Id, Rule: rule)))
            .Where(entry => entry.Rule.EndsAt > now)
            .GroupBy(entry => entry.Rule.RuleId)
            .Select(group =>
            {
                var rule = group.First().Rule;
                return new RuleDto(
                    RuleId: rule.RuleId,
                    ResourceIds: group.Select(entry => entry.ResourceId).ToList(),
                    Condition: new RuleConditionDto(rule.Condition.Field, rule.Condition.Operator, rule.Condition.Value),
                    Effect: new RuleEffectDto(rule.Effect.Type, rule.Effect.Value),
                    StartsAt: rule.StartsAt,
                    EndsAt: rule.EndsAt,
                    CreatedBy: rule.CreatedBy);
            })
            .OrderBy(rule => rule.StartsAt)
            .ToList();

        return new RulesResponse(rules);
    }

    public async Task<CreateRuleResponse> CreateAsync(string adminEmail, CreateRuleRequest request)
    {
        var now = DateTime.UtcNow;
        var startsAt = request.StartsAt.HasValue ? ToUtc(request.StartsAt.Value) : now;
        var endsAt = startsAt.AddHours(request.DurationHours);
        var effectType = request.Effect.Type!.Value;              // non null : [Required]
        var conditionOperator = request.Condition.Operator!.Value;

        // 400 : cohérence de la demande
        if (startsAt < now - StartTolerance)
            throw new BadRequestException("La date de début ne peut pas être dans le passé.");

        if (!string.Equals(request.Condition.Field, "price", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Seul le champ 'price' est supporté dans une condition.");

        var effectValue = ValidateEffectValue(effectType, request.Effect.Value);

        // 404 : toutes les ressources ciblées doivent exister
        var resourceIds = request.ResourceIds.Distinct().ToList();
        var resources = await _resourceRepository.GetByIdsWithoutHistoryAsync(resourceIds);
        var missing = resourceIds.Except(resources.Select(r => r.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Ressource(s) introuvable(s) : {string.Join(", ", missing)}.");

        foreach (var resource in resources)
        {
            var currentRules = resource.Rules.Where(rule => rule.EndsAt > now).ToList();

            // 409 : le même effet est déjà prévu sur une période qui chevauche
            if (currentRules.Any(rule => rule.Effect.Type == effectType
                                         && MarketRules.Overlaps(rule.StartsAt, rule.EndsAt, startsAt, endsAt)))
                throw new ConflictException($"{resource.Name} a déjà une règle {effectType} sur une période qui chevauche celle-ci.");

            // 422 : nombre maximum de règles actives ou à venir
            if (currentRules.Count >= GameSettings.MaxRulesPerResource)
                throw new BusinessRuleException($"{resource.Name} a déjà {GameSettings.MaxRulesPerResource} règles actives ou à venir (maximum atteint).");
        }

        var rule = new MarketRule
        {
            RuleId = Guid.NewGuid().ToString(),
            Condition = new RuleCondition { Field = "price", Operator = conditionOperator, Value = request.Condition.Value },
            Effect = new RuleEffect { Type = effectType, Value = effectValue },
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedBy = adminEmail,
            CreatedAt = now
        };

        await _resourceRepository.AddRuleAsync(resourceIds, rule);
        return new CreateRuleResponse(rule.RuleId);
    }

    public async Task DeleteAsync(string ruleId)
    {
        if (!await _resourceRepository.RemoveRuleAsync(ruleId))
            throw new NotFoundException($"Règle '{ruleId}' introuvable.");
    }

    // La valeur dépend du type d'effet
    private static decimal? ValidateEffectValue(EffectType type, decimal? value)
    {
        switch (type)
        {
            case EffectType.QuantityLimit:
                if (value is null || value < 1 || value != Math.Floor(value.Value))
                    throw new BadRequestException("QuantityLimit nécessite une valeur entière ≥ 1 (quantité max par transaction).");
                return value;

            case EffectType.TransactionTax:
                if (value is null || value <= 0 || value > GameSettings.MaxTaxPercent)
                    throw new BadRequestException($"TransactionTax nécessite un pourcentage entre 0 (exclu) et {GameSettings.MaxTaxPercent}.");
                return value;

            default:
                return null; // BuyBlocked / SellBlocked : pas de valeur
        }
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) // date sans fuseau : on la considère en UTC
    };
}