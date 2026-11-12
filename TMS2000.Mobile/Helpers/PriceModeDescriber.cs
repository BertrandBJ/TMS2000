using TMS2000.Shared;

namespace TMS2000.Mobile.Helpers;

public static class PriceModeDescriber
{
    public static string Describe(PriceMode priceMode) => priceMode switch
    {
        PriceMode.Stable => "Stable",
        PriceMode.SlowRise => "Hausse lente",
        PriceMode.FastRise => "Hausse rapide",
        PriceMode.SlowFall => "Baisse lente",
        PriceMode.FastFall => "Baisse rapide",
        PriceMode.Chaotic => "Erratique",
        _ => "Inconnue"
    };
}
