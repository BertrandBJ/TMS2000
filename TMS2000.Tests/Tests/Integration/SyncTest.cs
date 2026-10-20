using System.Net;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Tests.Infrastructure;
using Xunit;
using static TMS2000.Tests.Infrastructure.ApiCalls;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class SyncTests
{
    // Historique de r-carrot posé par la fixture : 20 à SeedTime-2h, 30 à SeedTime-1h, 25 à SeedTime.
    private const string Carrot = "r-carrot";
    private const decimal TestResourcePrice = 50m;

    private readonly TestApiFactory _api;

    public SyncTests(TestApiFactory api) => _api = api;

    [Fact]
    public async Task Sync_BuyThenSell_ReplaysAtHistoricalPrices_AndGrantsOfflineBonus()
    {
        var player = await _api.CreatePlayerClientAsync();
        var buy = NewAction(Carrot, TransactionType.Buy, 5, _api.SeedTime.AddHours(-2));   // 5 × 20 = 100
        var sell = NewAction(Carrot, TransactionType.Sell, 5, _api.SeedTime.AddHours(-1)); // 5 × 30 = 150

        // Envoyées volontairement dans le désordre : le serveur doit les rejouer par date.
        var response = await PostSyncAsync(player, sell, buy);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sync = await ReadAsync<SyncResponse>(response);

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
    public async Task Sync_InvalidActions_AreRejectedOneByOne_WithoutTouchingBalance()
    {
        var player = await _api.CreatePlayerClientAsync();
        var now = DateTime.UtcNow;

        var response = await PostSyncAsync(player,
            NewAction("r-unknown", TransactionType.Buy, 1, now),                    // ressource inconnue
            NewAction("r-gold", TransactionType.Buy, 4, _api.SeedTime),              // 4 × 300 = 1200 > 1000
            NewAction(Carrot, TransactionType.Buy, 1, now.AddHours(1)),              // dans le futur
            NewAction(Carrot, TransactionType.Buy, 1, now.AddDays(-40)),             // trop ancienne
            NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime.AddHours(-5)));  // avant le premier prix connu

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sync = await ReadAsync<SyncResponse>(response);
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
    public async Task Sync_SameBatchTwice_IsAppliedOnlyOnce()
    {
        var player = await _api.CreatePlayerClientAsync();
        var buy = NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime); // 1 × 25

        var first = await ReadAsync<SyncResponse>(await PostSyncAsync(player, buy));
        var second = await ReadAsync<SyncResponse>(await PostSyncAsync(player, buy));

        Assert.Equal(first.Results, second.Results);
        var profile = await GetProfileAsync(player);
        Assert.Equal(TestApiFactory.PlayerStartingBalance - 25m, profile.Balance);
    }

    [Fact]
    public async Task Sync_AppliesTheRulesActiveAtEachActionDate()
    {
        var player = await _api.CreatePlayerClientAsync();
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var beforeRule = DateTime.UtcNow;
        var admin = await _api.CreateAdminClientAsync();
        var ruleResponse = await CreateRuleAsync(admin, resourceId, EffectType.BuyBlocked);
        ruleResponse.EnsureSuccessStatusCode();

        var buyBeforeRule = NewAction(resourceId, TransactionType.Buy, 1, beforeRule);
        var buyDuringRule = NewAction(resourceId, TransactionType.Buy, 1, DateTime.UtcNow);
        var sync = await ReadAsync<SyncResponse>(await PostSyncAsync(player, buyBeforeRule, buyDuringRule));

        Assert.Equal(TransactionStatus.Accepted, ResultOf(sync, buyBeforeRule).Status);
        Assert.Equal(TransactionStatus.Rejected, ResultOf(sync, buyDuringRule).Status);
    }

    [Fact]
    public async Task Sync_WithoutActions_Returns400()
    {
        var player = await _api.CreatePlayerClientAsync();

        var response = await PostSyncAsync(player);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sync_WithMissingDate_Returns400()
    {
        var player = await _api.CreatePlayerClientAsync();

        var response = await PostSyncAsync(player, new SyncAction(Guid.NewGuid().ToString(), Carrot, TransactionType.Buy, 1, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sync_AsAdmin_Returns403()
    {
        var admin = await _api.CreateAdminClientAsync();

        var response = await PostSyncAsync(admin, NewAction(Carrot, TransactionType.Buy, 1, _api.SeedTime));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Helpers ---

    private static SyncAction NewAction(string resourceId, TransactionType type, int quantity, DateTime createdAt) =>
        new(Guid.NewGuid().ToString(), resourceId, type, quantity, createdAt);

    private static SyncActionResult ResultOf(SyncResponse sync, SyncAction action) =>
        sync.Results.Single(result => result.TransactionId == action.TransactionId);
}