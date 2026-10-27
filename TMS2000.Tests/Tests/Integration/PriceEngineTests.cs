using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using TMS2000.Core;
using TMS2000.Core.Data;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;
using TMS2000.Core.Services;
using TMS2000.Server.BackgroundServices;
using TMS2000.Shared;
using TMS2000.Tests.Infrastructure;
using Xunit;

namespace TMS2000.Tests.Tests.Integration;

// Le moteur de prix fait évoluer TOUTES les ressources de la base : il tourne ici sur une base à part,
// pour ne pas modifier l'autre bdd de test on la met sur le coté avec une autre bbd.

public class PriceEngineTests : IAsyncLifetime
{
    private const string DatabaseName = "tms2000_price_engine_tests";

    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Resource> _resources;

    public PriceEngineTests()
    {
        // Même source de connexion que TestApiFactory : Mongo de dctest en local, service mongo en CI
        var mongoConnectionString = Environment.GetEnvironmentVariable("TMS2000_TEST_MONGO")
                                    ?? "mongodb://localhost:27018";

        MongoConventions.Register();
        _database = new MongoClient(mongoConnectionString).GetDatabase(DatabaseName);
        _resources = _database.GetCollection<Resource>(MongoCollections.Resources);
    }

    // xUnit crée une instance de cette classe PAR test : chaque test démarre sur une base vide
    public Task InitializeAsync() => _database.Client.DropDatabaseAsync(DatabaseName);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Tick_UpdatesEveryResource_AndAppendsOnePricePoint()
    {
        var firstPointAt = TruncateToMilliseconds(DateTime.UtcNow.AddHours(-1));
        var initialModeByResourceId = new Dictionary<string, PriceMode>
        {
            ["r-tick-rise"] = PriceMode.FastRise,
            ["r-tick-chaotic"] = PriceMode.Chaotic
        };
        foreach (var (resourceId, initialMode) in initialModeByResourceId)
            await InsertResourceAsync(resourceId, 50m, initialMode, firstPointAt, historyLength: 3);

        var tickTime = TruncateToMilliseconds(DateTime.UtcNow);
        await CreatePriceEngine().TickAsync(tickTime);

        var resources = await _resources.Find(FilterDefinition<Resource>.Empty).ToListAsync();
        Assert.Equal(2, resources.Count);
        foreach (var resource in resources)
        {
            Assert.Equal(4, resource.PriceHistory.Count);           // exactement un point ajouté

            var lastPoint = resource.PriceHistory[^1];
            Assert.Equal(tickTime, lastPoint.At);                   // daté du tick
            Assert.Equal(resource.CurrentPrice, lastPoint.Price);   // le prix courant est le dernier point

            // ModeChangedAt ne bouge que si le tick a tiré un nouveau mode
            var modeChanged = resource.Mode != initialModeByResourceId[resource.Id];
            Assert.Equal(modeChanged ? tickTime : firstPointAt, resource.ModeChangedAt);
        }
    }

    [Fact]
    public async Task Tick_OnFullHistory_DropsTheOldestPoint()
    {
        const string resourceId = "r-full-history";
        var firstPointAt = TruncateToMilliseconds(DateTime.UtcNow.AddMinutes(-(GameSettings.MaxPricePoints + 10)));
        await InsertResourceAsync(resourceId, 50m, PriceMode.Stable, firstPointAt, GameSettings.MaxPricePoints);

        var tickTime = TruncateToMilliseconds(DateTime.UtcNow);
        await CreatePriceEngine().TickAsync(tickTime);

        var resource = await _resources.Find(r => r.Id == resourceId).FirstAsync();
        Assert.Equal(GameSettings.MaxPricePoints, resource.PriceHistory.Count); // taille toujours plafonnée
        Assert.Equal(firstPointAt.AddMinutes(1), resource.PriceHistory[0].At);  // le plus ancien point a disparu
        Assert.Equal(tickTime, resource.PriceHistory[^1].At);                   // le nouveau est à la fin
    }

    [Fact]
    public async Task TickService_AppliesTicksOnItsOwn_AndAnnouncesNextTick()
    {
        const string resourceId = "r-timer";
        await InsertResourceAsync(resourceId, 50m, PriceMode.Stable, TruncateToMilliseconds(DateTime.UtcNow), historyLength: 1);

        // Mêmes enregistrements que Program.cs, mais branchés sur la base de ce test
        var services = new ServiceCollection();
        services.AddSingleton(_database);
        services.AddScoped<ResourceRepository>();
        services.AddScoped<PriceEngineService>();
        await using var serviceProvider = services.BuildServiceProvider();

        var marketClock = new MarketClock();
        var tickInterval = TimeSpan.FromMilliseconds(200);    // au lieu de 30 s en dev
        var tickService = new PriceTickService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            marketClock,
            NullLogger<PriceTickService>.Instance,
            tickInterval);

        var serviceStartedAt = DateTime.UtcNow;
        await tickService.StartAsync(CancellationToken.None);
        try
        {
            // Attente bornée : 2 ticks prennent ~400 ms, on laisse 10 s de marge pour une CI chargée
            var waitDeadline = DateTime.UtcNow.AddSeconds(10);
            var historyLength = 1;
            while (historyLength < 3 && DateTime.UtcNow < waitDeadline)
            {
                await Task.Delay(50);
                historyLength = (await _resources.Find(r => r.Id == resourceId).FirstAsync()).PriceHistory.Count;
            }

            Assert.True(historyLength >= 3, $"Au moins 2 ticks attendus en 10 s, historique : {historyLength} point(s).");
            Assert.True(marketClock.NextTickAt >= serviceStartedAt + tickInterval, "NextTickAt doit annoncer le prochain tick.");
        }
        finally
        {
            // Toujours arrêter le service, même si une assertion échoue : sinon il continue de tourner
            await tickService.StopAsync(CancellationToken.None);
        }
    }

    // --- Helpers ---

    private PriceEngineService CreatePriceEngine() => new(new ResourceRepository(_database));

    // Un point par minute à partir de firstPointAt, tous au même prix
    private async Task InsertResourceAsync(string resourceId, decimal price, PriceMode mode, DateTime firstPointAt, int historyLength)
    {
        var priceHistory = Enumerable.Range(0, historyLength)
            .Select(minuteOffset => new PricePoint { Price = price, At = firstPointAt.AddMinutes(minuteOffset) })
            .ToList();

        await _resources.InsertOneAsync(new Resource
        {
            Id = resourceId,
            Name = resourceId,
            CurrentPrice = price,
            Mode = mode,
            ModeChangedAt = firstPointAt,
            PriceHistory = priceHistory
        });
    }

    // MongoDB stocke les dates à la milliseconde : sans cette troncature, la date relue
    // ne serait plus égale à celle envoyée (DateTime est précis au dixième de microseconde)
    private static DateTime TruncateToMilliseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
}