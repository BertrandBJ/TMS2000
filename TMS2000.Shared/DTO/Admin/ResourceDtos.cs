using System.ComponentModel.DataAnnotations;

namespace TMS2000.Shared.DTO.Admin;

public record UpdatePriceBandRequest(
    [Range(0.01, 1_000_000)] decimal BasePrice,
    [Range(0.01, 1_000_000)] decimal? MinPrice,
    [Range(0.01, 3_000_000)] decimal? MaxPrice);

public record AdminResourceDto(
    string Id,
    string Name,
    decimal CurrentPrice,
    PriceMode Mode,
    decimal BasePrice,
    decimal MinPrice,
    decimal MaxPrice);

public record AdminResourcesResponse(List<AdminResourceDto> Resources);
