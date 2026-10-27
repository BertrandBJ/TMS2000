using TMS2000.Shared;

namespace TMS2000.Core.Helper;

public static class PriceSimulator
{
    private const double ModeChangeProbability = 0.15;


    // below : synthax : next => nom de la methode (parameter it takes).
    public static (decimal Price, PriceMode Mode) Next(decimal currentPrice, PriceMode mode, Random random)
    {
        var (drift, volatility) = mode switch
        // on explose le tuple instant, pas besoin de le nommée
        {
            PriceMode.Stable => (0.0, 0.5),
            PriceMode.SlowRise => (0.5, 1.0),
            PriceMode.FastRise => (2.0, 2.0),
            PriceMode.SlowFall => (-0.5, 1.0),
            PriceMode.FastFall => (-2.0, 2.0),
            PriceMode.Chaotic => (0.0, 6.0),
            _ => (0.0, 0.5)
        };

        // tendance + bruit aléatoire entre -volatility et +volatility. pour le calcule : NextDouble()x2-1 ça donne une valeur entre -1 et 1. 

        var changePercent = drift + (random.NextDouble() * 2 - 1) * volatility;

        var newPrice = Math.Round(currentPrice * (1 + (decimal)changePercent / 100m), 2);

        if (newPrice < GameSettings.MinPrice)
            newPrice = GameSettings.MinPrice;

        var newMode = random.NextDouble() < ModeChangeProbability ? RandomMode(random) : mode;

        return (newPrice, newMode);
    }
    // below selection du modeRandomly si jamais ça passe le if above (d'environ 12,5%).

    private static PriceMode RandomMode(Random random)
    {
        var modes = Enum.GetValues<PriceMode>();
        return modes[random.Next(modes.Length)];
        // donne un index entre 0-5. (l'qui va indiquer depusi l'enum above et stocked dans modes above.
        // alors attention ici le Next n'est pas la même methode que celle above. C'est un truc du framework.net. C'est un random entre 0(int).
    }
}