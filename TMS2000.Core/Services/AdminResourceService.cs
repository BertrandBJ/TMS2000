using TMS2000.Core.Exceptions;
using TMS2000.Core.Helper;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Core.Services;

public class AdminResourceService
{
    private readonly ResourceRepository _resourceRepository;

    public AdminResourceService(ResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<AdminResourcesResponse> GetResourcesAsync()
    {
        var resources = await _resourceRepository.GetAllWithoutHistoryAsync();

        return new AdminResourcesResponse(resources
            .OrderBy(resource => resource.BasePrice)
            .ThenBy(resource => resource.Name)
            .Select(ToAdminResourceDto)
            .ToList());
    }

    public async Task<AdminResourceDto> UpdatePriceBandAsync(string resourceId, UpdatePriceBandRequest request)
    {
        var defaultBand = PriceBand.FromBasePrice(request.BasePrice);
        var priceBand = new PriceBand(
            request.BasePrice,
            request.MinPrice ?? defaultBand.MinPrice,
            request.MaxPrice ?? defaultBand.MaxPrice);

        if (priceBand.MinPrice < GameSettings.MinPrice)
            throw new BadRequestException($"Le prix minimum doit être d'au moins {GameSettings.MinPrice}.");

        if (!priceBand.IsDefined)
            throw new BadRequestException("La fourchette doit respecter : prix minimum ≤ prix de référence ≤ prix maximum.");

        if (!await _resourceRepository.UpdatePriceBandAsync(resourceId, priceBand.BasePrice, priceBand.MinPrice, priceBand.MaxPrice))
            throw new NotFoundException($"Ressource '{resourceId}' introuvable.");

        var updatedResources = await _resourceRepository.GetByIdsWithoutHistoryAsync(new[] { resourceId });
        return ToAdminResourceDto(updatedResources.Single());
    }

    private static AdminResourceDto ToAdminResourceDto(Resource resource) => new(
        Id: resource.Id,
        Name: resource.Name,
        CurrentPrice: resource.CurrentPrice,
        Mode: resource.Mode,
        BasePrice: resource.BasePrice,
        MinPrice: resource.MinPrice,
        MaxPrice: resource.MaxPrice);
}
