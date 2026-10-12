using TMS2000.Shared;

namespace TMS2000.Core.Helper;

public static class PriceSimulator
{
    private const double ModeChangeProbability = 0.15;

    public static (decimal Price, PriceMode Mode) Next(decimal currentPrice, PriceMode mode, Random random)
    {
        var (drift, volatility) = mode switch
        {
            PriceMode.Stable => (0.0, 0.5),
            PriceMode.SlowRise => (0.5, 1.0),
            PriceMode.FastRise => (2.0, 2.0),
            PriceMode.SlowFall => (-0.5, 1.0),
            PriceMode.FastFall => (-2.0, 2.0),
            PriceMode.Chaotic => (0.0, 6.0),
            _ => (0.0, 0.5)
        };

        // tendance + bruit aléatoire entre -volatility et +volatility
        var changePercent = drift + (random.NextDouble() * 2 - 1) * volatility;

        var newPrice = Math.Round(currentPrice * (1 + (decimal)changePercent / 100m), 2);
        if (newPrice < GameSettings.MinPrice)
            newPrice = GameSettings.MinPrice;

        var newMode = random.NextDouble() < ModeChangeProbability ? RandomMode(random) : mode;

        return (newPrice, newMode);
    }

    private static PriceMode RandomMode(Random random)
    {
        var modes = Enum.GetValues<PriceMode>();
        return modes[random.Next(modes.Length)];
    }
}