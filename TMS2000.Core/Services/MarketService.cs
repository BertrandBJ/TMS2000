using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Market;

namespace TMS2000.Core.Services;

public class MarketService
{
    private readonly ResourceRepository _resourceRepository;
    private readonly MarketClock _marketClock;

    public MarketService(ResourceRepository resourceRepository, MarketClock marketClock)
    {
        _resourceRepository = resourceRepository;
        _marketClock = marketClock;
    }

    public async Task<MarketResponse> GetMarketAsync()
    {
        var now = DateTime.UtcNow;
        var resources = await _resourceRepository.GetAllWithLatestPricesAsync();

        return new MarketResponse(
            NextTickAt: _marketClock.NextTickAt,
            Resources: resources.Select(resource => ToDto(resource, now)).ToList());
    }

    public async Task<MarketResourceDto> GetResourceAsync(string resourceId)
    {
        var resource = await _resourceRepository.GetWithLatestPricesAsync(resourceId)
                       ?? throw new NotFoundException($"Ressource '{resourceId}' introuvable.");

        return ToDto(resource, DateTime.UtcNow);
    }

    public async Task<PriceHistoryResponse> GetHistoryAsync(string resourceId, int hours)
    {
        var resource = await _resourceRepository.GetByIdAsync(resourceId)
                       ?? throw new NotFoundException($"Ressource '{resourceId}' introuvable.");

        var since = DateTime.UtcNow.AddHours(-hours);
        var points = resource.PriceHistory
            .Where(point => point.At >= since)
            .Select(point => new PricePointDto(point.Price, point.At))
            .ToList();

        return new PriceHistoryResponse(resource.Id, points);
    }

    private static MarketResourceDto ToDto(Resource resource, DateTime now) => new(
        Id: resource.Id,
        Name: resource.Name,
        CurrentPrice: resource.CurrentPrice,
        Variation: ComputeVariation(resource.PriceHistory),
        CanBuy: MarketRules.CanBuy(resource.Rules, now, resource.CurrentPrice),
        CanSell: MarketRules.CanSell(resource.Rules, now, resource.CurrentPrice),
        ActiveRules: resource.Rules
            .Where(rule => MarketRules.IsActive(rule, now))
            .Select(rule => new ActiveRuleDto(MarketRules.Describe(rule), rule.EndsAt))
            .ToList());

    // Variation en % entre les deux derniers points d'historique
    private static decimal ComputeVariation(List<PricePoint> history)
    {
        if (history.Count < 2)
            return 0m;

        var previous = history[^2].Price;
        var last = history[^1].Price;

        return previous == 0m ? 0m : Math.Round((last - previous) / previous * 100m, 2);
    }
}