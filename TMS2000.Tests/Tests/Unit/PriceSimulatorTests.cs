using TMS2000.Core;
using TMS2000.Core.Helper;
using TMS2000.Shared;
using Xunit;

namespace TMS2000.Tests.Tests.Unit;

// Tests unitaires : PriceSimulator est une fonction pure (le Random est passé en paramètre),
// donc ni API ni base de données. Pas de [Collection] : ces tests tournent en parallèle
// de la suite d'intégration sans la gêner.


public class PriceSimulatorTests
{
    private const int SampleSize = 1000;                       // nombre de tick serveur à simuler par les test.
    private const int RandomSeed = 42;                       // graine fixe : les mêmes tirages à chaque exécution
    private const decimal ReferencePrice = 100m;             // à 100, 1 % de variation = 1 unité de prix
    private const decimal RoundingMargin = 0.01m;            // le prix est arrondi au centime
    private const decimal AverageTrendTolerancePercent = 0.25m;

    // Chaque mode : tendance (drift) et amplitude du bruit (volatility), en % par tick.
    // Ce tableau est la spécification : s'il change dans PriceSimulator, ce test doit échouer.
    [Theory]
    [InlineData(PriceMode.Stable, 0.0, 0.5)]
    [InlineData(PriceMode.SlowRise, 0.5, 1.0)]
    [InlineData(PriceMode.FastRise, 2.0, 2.0)]
    [InlineData(PriceMode.SlowFall, -0.5, 1.0)]
    [InlineData(PriceMode.FastFall, -2.0, 2.0)]
    [InlineData(PriceMode.Chaotic, 0.0, 6.0)]
    public void Next_StaysWithinModeBounds_AndFollowsModeTrend(PriceMode mode, double driftPercent, double volatilityPercent)
    {
        var random = new Random(RandomSeed);
        var lowestAllowedPrice = ReferencePrice * (1 + (decimal)(driftPercent - volatilityPercent) / 100m) - RoundingMargin;
        var highestAllowedPrice = ReferencePrice * (1 + (decimal)(driftPercent + volatilityPercent) / 100m) + RoundingMargin;
        var totalChangePercent = 0m;

        //below c'est là qu'on fait run le next en fonction du sampleSize. on simule le running time du serveur.
        for (var sampleIndex = 0; sampleIndex < SampleSize; sampleIndex++)
        {
            var (newPrice, _) = PriceSimulator.Next(ReferencePrice, mode, random);

            Assert.InRange(newPrice, lowestAllowedPrice, highestAllowedPrice);
            Assert.Equal(decimal.Round(newPrice, 2), newPrice); // jamais plus de 2 décimales
            totalChangePercent += (newPrice - ReferencePrice) / ReferencePrice * 100m;
        }

        // Le bruit est symétrique autour de 0 : en moyenne, la variation tend vers la tendance du mode
        var averageChangePercent = totalChangePercent / SampleSize;
        Assert.InRange(averageChangePercent,
            (decimal)driftPercent - AverageTrendTolerancePercent,
            (decimal)driftPercent + AverageTrendTolerancePercent);
    }

    [Fact]
    public void Next_NeverGoesBelowMinPrice_WhateverTheMode()
    {
        var random = new Random(RandomSeed);

        foreach (var mode in Enum.GetValues<PriceMode>())
        {
            for (var sampleIndex = 0; sampleIndex < SampleSize; sampleIndex++)
            {
                // Départ au prix plancher : toute baisse doit être bloquée à MinPrice
                var (newPrice, _) = PriceSimulator.Next(GameSettings.MinPrice, mode, random);

                Assert.True(newPrice >= GameSettings.MinPrice, $"{mode} : prix {newPrice} sous le plancher");
            }
        }
    }

    [Fact]
    public void Next_ChangesModeSometimes_ButNotEveryTick()
    {
        var random = new Random(RandomSeed);
        var modeChangeCount = 0;

        for (var sampleIndex = 0; sampleIndex < SampleSize; sampleIndex++)
        {
            var (_, newMode) = PriceSimulator.Next(ReferencePrice, PriceMode.Stable, random);
            if (newMode != PriceMode.Stable)
                modeChangeCount++;
        }

        // 15 % de chance de tirer un nouveau mode, dont 5 sur 6 différents du mode actuel : environ 12,5 %
        Assert.InRange(modeChangeCount, SampleSize * 5 / 100, SampleSize * 20 / 100);
    }
}