using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;

namespace TMS2000.Core.Services;

public class PriceEngineService
{
    private readonly ResourceRepository _resourceRepository;

    public PriceEngineService(ResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task TickAsync(DateTime now)
    {
        var resources = await _resourceRepository.GetAllWithoutHistoryAsync();

        foreach (var resource in resources)
        {
            var (newPrice, newMode) = PriceSimulator.Next(resource.CurrentPrice, resource.Mode, Random.Shared);
            var modeChangedAt = newMode != resource.Mode ? now : resource.ModeChangedAt;

            await _resourceRepository.ApplyTickAsync(
                resource.Id, newPrice, newMode, modeChangedAt,
                new PricePoint { Price = newPrice, At = now });
        }
    }
}