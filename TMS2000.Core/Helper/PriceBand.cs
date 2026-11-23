using TMS2000.Core.Models;

namespace TMS2000.Core.Helper;

public readonly record struct PriceBand(decimal BasePrice, decimal MinPrice, decimal MaxPrice)
{
    public static readonly PriceBand Undefined = new(0m, 0m, 0m);

    public bool IsDefined => BasePrice > 0 && MinPrice > 0 && MinPrice <= BasePrice && BasePrice <= MaxPrice;

    public static PriceBand For(Resource resource) =>
        new(resource.BasePrice, resource.MinPrice, resource.MaxPrice);

    public static PriceBand FromBasePrice(decimal basePrice) =>
        new(
            basePrice,
            Math.Max(GameSettings.MinPrice, Math.Round(basePrice * GameSettings.DefaultMinPriceMultiplier, 2)),
            Math.Round(basePrice * GameSettings.DefaultMaxPriceMultiplier, 2));

    public decimal Clamp(decimal price)
    {
        if (!IsDefined)
        {
            return Math.Max(price, GameSettings.MinPrice);
        }

        return Math.Clamp(price, MinPrice, MaxPrice);
    }

    public bool Contains(decimal price) => !IsDefined || (price >= MinPrice && price <= MaxPrice);
}
