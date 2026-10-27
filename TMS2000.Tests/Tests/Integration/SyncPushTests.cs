using System.Net;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Tests.Infrastructure;
using Xunit;
using static TMS2000.Tests.Infrastructure.ApiCalls;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class SyncPushTests
{
    // Historique de r-carrot posé par la fixture : 20 à SeedTime-2h, 30 à SeedTime-1h, 25 à SeedTime.

    private const string Carrot = "r-carrot";
    private const decimal TestResourcePrice = 50m;

    private readonly TestApiFactory _api;

    public SyncPushTests(TestApiFactory api) => _api = api;

    [Fact]
    public async Task Push_BuyThenSell_ReplaysAtHistoricalPrices_AndGrantsOfflineBonus()
    {
        var player = await _api.CreatePlayerClientAsync();
        var buy = NewAction(Carrot, TransactionType.Buy, 5, _api.SeedTime.AddHours(-2));   // 5 × 20 = 100
        var sell = NewAction(Carrot, TransactionType.Sell, 5, _api.SeedTime.AddHours(-1)); // 5 × 30 = 150

        // Envoyées volontairement dans le désordre : le serveur doit les rejouer par date.
        var response = await PostSyncPushAsync(player, sell, buy);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sync = await ReadAsync<SyncPushResponse>(response);

        Assert.All(sync.Results, result => Assert.Equal(TransactionStatus.Accepted, result.Status));
        Assert.Equal(20m, ResultOf(sync, buy).ExecutedPrice);  // prix à SeedTime-2h
        Assert.Equal(30m, ResultOf(sync, sell).ExecutedPrice); // prix à SeedTime-1h

        var sellResult = ResultOf(sync, sell);
        Assert.Equal(50m, sellResult.Profit);
        Assert.Equal(1m, sellResult.Efficiency); // vendu au meilleur prix de la période de détention
        var bonus = sellResult.OfflineBonus ?? 0m;
        Assert.True(bonus > 0m, "Une vente gagnante hors ligne doit rapporter un bonus.");

        var expectedBalance = TestApiFactory.PlayerStartingBalance + 50m + bonus;
        Assert.Equal(expectedBalance, sync.NewBalance);                        // ce que la synchro annonce
        Assert.Equal(expectedBalance, (await GetProfileAsync(player)).Balance); // ce qui est réellement en base
    }

    [Fact]
    public async Task Push_InvalidActions_AreRejectedOneByOne_WithoutTouchingBalance()
    {
        var player = await _api.CreatePlayerClientAsync();
        var now = DateTime.UtcNow;

        var response = await PostSyncPushAsync(player,
            NewAction("r-unknown", TransactionType.Buy, 1, now),                    // ressource inconnue
            NewAction("r-gold", TransactionType.Buy, 4, _api.SeedTime),              // 4 × 300 = 1200 > 1000
            NewAction(Carrot, TransactionType.Buy, 1, now.AddHours(1)),              // dans le futur
            NewAction(Carrot, TransactionType.Buy, 1, now.AddDays(-40)),             // trop ancienne
            NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime.AddHours(-5)));  // avant le premier prix connu

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sync = await ReadAsync<SyncPushResponse>(response);
        Assert.Equal(5, sync.Results.Count());
        Assert.All(sync.Results, result =>
        {
            Assert.Equal(TransactionStatus.Rejected, result.Status);
            Assert.False(string.IsNullOrWhiteSpace(result.RejectReason));
        });

        var profile = await GetProfileAsync(player);
        Assert.Equal(TestApiFactory.PlayerStartingBalance, profile.Balance);
    }

    [Fact]
    public async Task Push_SameBatchTwice_IsAppliedOnlyOnce()
    {
        var player = await _api.CreatePlayerClientAsync();
        var buy = NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime); // 1 × 25

        var first = await ReadAsync<SyncPushResponse>(await PostSyncPushAsync(player, buy));
        var second = await ReadAsync<SyncPushResponse>(await PostSyncPushAsync(player, buy));

        Assert.Equal(first.Results, second.Results);
        var profile = await GetProfileAsync(player);
        Assert.Equal(TestApiFactory.PlayerStartingBalance - 25m, profile.Balance);
    }

    [Fact]
    public async Task Push_AppliesTheRulesActiveAtEachActionDate()
    {
        var player = await _api.CreatePlayerClientAsync();
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var beforeRule = DateTime.UtcNow;
        var admin = await _api.CreateAdminClientAsync();
        var ruleResponse = await CreateRuleAsync(admin, resourceId, EffectType.BuyBlocked);
        ruleResponse.EnsureSuccessStatusCode();

        var buyBeforeRule = NewAction(resourceId, TransactionType.Buy, 1, beforeRule);
        var buyDuringRule = NewAction(resourceId, TransactionType.Buy, 1, DateTime.UtcNow);
        var sync = await ReadAsync<SyncPushResponse>(await PostSyncPushAsync(player, buyBeforeRule, buyDuringRule));

        Assert.Equal(TransactionStatus.Accepted, ResultOf(sync, buyBeforeRule).Status);
        Assert.Equal(TransactionStatus.Rejected, ResultOf(sync, buyDuringRule).Status);
    }

    [Fact]
    public async Task Push_WithoutActions_Returns400()
    {
        var player = await _api.CreatePlayerClientAsync();

        var response = await PostSyncPushAsync(player);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Push_WithMissingDate_Returns400()
    {
        var player = await _api.CreatePlayerClientAsync();

        var response = await PostSyncPushAsync(player, new OfflineAction(Guid.NewGuid().ToString(), Carrot, TransactionType.Buy, 1, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Push_AsAdmin_Returns403()
    {
        var admin = await _api.CreateAdminClientAsync();

        var response = await PostSyncPushAsync(admin, NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Lots FIFO : une vente consomme d'abord les lots les plus anciens.
    // r-carrot vaut 20 à SeedTime-2h, 30 à SeedTime-1h, 25 à SeedTime.
    [Fact]
    public async Task Push_SaleConsumesOldestLotsFirst()
    {
        var player = await _api.CreatePlayerClientAsync();
        var buyCheapLot = NewAction(Carrot, TransactionType.Buy, 2, _api.SeedTime.AddHours(-2));  // 2 × 20
        var buyExpensiveLot = NewAction(Carrot, TransactionType.Buy, 2, _api.SeedTime.AddHours(-1)); // 2 × 30
        var sellThree = NewAction(Carrot, TransactionType.Sell, 3, _api.SeedTime);                // 3 × 25 = 75

        var sync = await ReadAsync<SyncPushResponse>(await PostSyncPushAsync(player, buyCheapLot, buyExpensiveLot, sellThree));

        var saleResult = ResultOf(sync, sellThree);
        Assert.Equal(TransactionStatus.Accepted, saleResult.Status);

        // FIFO : coût des 3 unités vendues = 2 × 20 + 1 × 30 = 70 → profit 75 - 70 = 5
        // (en LIFO, ce serait 2 × 30 + 1 × 20 = 80 → profit -5)
        Assert.Equal(5m, saleResult.Profit);

        // Profit max possible : lot à 20 → meilleur prix 30 pendant sa détention : 2 × (30 - 20) = 20
        //                       lot à 30 → meilleur prix 30 : 1 × (30 - 30) = 0
        // Efficacité = 5 / 20
        Assert.Equal(0.25m, saleResult.Efficiency);

        // Il reste 1 unité, issue du lot acheté à 30
        var remainingCarrots = Assert.Single((await GetProfileAsync(player)).Portfolio);
        Assert.Equal(1, remainingCarrots.Quantity);
        Assert.Equal(30m, remainingCarrots.AverageCost);
    }

    // Au rejeu, on ne peut vendre que ce qu'on possédait à la date de la vente
    [Fact]
    public async Task Push_SaleDatedBeforeThePurchase_IsRejected()
    {
        var player = await _api.CreatePlayerClientAsync();
        var buy = NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime.AddHours(-1));
        var sellBeforeBuying = NewAction(Carrot, TransactionType.Sell, 1, _api.SeedTime.AddHours(-2));

        // Envoyées dans l'ordre "achat puis vente" : le serveur doit quand même rejouer par date
        var sync = await ReadAsync<SyncPushResponse>(await PostSyncPushAsync(player, buy, sellBeforeBuying));

        Assert.Equal(TransactionStatus.Rejected, ResultOf(sync, sellBeforeBuying).Status);
        Assert.Equal(TransactionStatus.Accepted, ResultOf(sync, buy).Status);
        Assert.Equal(1, Assert.Single((await GetProfileAsync(player)).Portfolio).Quantity); // l'unité achetée est toujours là
    }

    // --- Helpers ---

    private static OfflineAction NewAction(string resourceId, TransactionType type, int quantity, DateTime createdAt) =>
        new(Guid.NewGuid().ToString(), resourceId, type, quantity, createdAt);

    private static OfflineActionResult ResultOf(SyncPushResponse sync, OfflineAction action) =>
        sync.Results.Single(result => result.TransactionId == action.TransactionId);
}