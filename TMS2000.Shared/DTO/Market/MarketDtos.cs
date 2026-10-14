namespace TMS2000.Shared.DTO.Market;

public record MarketResponse(
    DateTime NextTickAt,
    List<MarketResourceDto> Resources);

public record MarketResourceDto(
    string Id,
    string Name,
    decimal CurrentPrice,
    decimal Variation,              // en % par rapport au tick précédent
    bool CanBuy,
    bool CanSell,
    List<ActiveRuleDto> ActiveRules);

public record ActiveRuleDto(
    string Description,
    DateTime EndsAt);

public record PriceHistoryResponse(
    string ResourceId,
    List<PricePointDto> Points);

public record PricePointDto(
    decimal Price,
    DateTime At);