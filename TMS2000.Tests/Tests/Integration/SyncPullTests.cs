using System.Net;
using System.Net.Http.Json;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Tests.Infrastructure;
using Xunit;
using static TMS2000.Tests.Infrastructure.ApiCalls;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class SyncPullTests
{
    // Historique de r-carrot posé par la fixture : 20 à SeedTime-2h, 30 à SeedTime-1h, 25 à SeedTime
    private const string Carrot = "r-carrot";
    private const decimal CarrotPrice = 25m;
    private const decimal TestResourcePrice = 50m;

    private readonly TestApiFactory _api;

    public SyncPullTests(TestApiFactory api) => _api = api;

    [Fact]
    public async Task Snapshot_ContainsPlayerState_AndMarket()
    {
        var playerClient = await _api.CreatePlayerClientAsync();
        await PostTradeAsync(playerClient, "buy-1", Carrot, TransactionType.Buy, 4);

        var snapshot = await GetSnapshotAsync(playerClient);

        // Le joueur : solde, lots et transactions tels qu'en base
        Assert.Equal(TestApiFactory.PlayerStartingBalance - 4 * CarrotPrice, snapshot.Player.Balance);
        var carrotLot = Assert.Single(Assert.Single(snapshot.Player.Portfolio).Lots);
        Assert.Equal(4, carrotLot.Quantity);
        Assert.Equal(CarrotPrice, carrotLot.BuyPrice);
        Assert.Equal("buy-1", Assert.Single(snapshot.Player.Transactions).TransactionId);

        // Le marché : prix actuel et derniers points d'historique
        var carrot = snapshot.Resources.Single(resource => resource.Id == Carrot);
        Assert.Equal(CarrotPrice, carrot.CurrentPrice);
        Assert.Equal(new[] { 20m, 30m, 25m }, carrot.RecentPrices.Select(pricePoint => pricePoint.Price));

        // Les horloges : le prochain tick est annoncé après l'heure du serveur
        Assert.True(snapshot.NextTickAt > snapshot.ServerTime);
    }

    [Fact]
    public async Task Snapshot_ContainsActiveAndUpcomingRules_WithTheirConditionAndEffect()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        var playerClient = await _api.CreatePlayerClientAsync();
        var upcomingStart = DateTime.UtcNow.AddHours(1);
        (await CreateRuleAsync(adminClient, resourceId, EffectType.BuyBlocked)).EnsureSuccessStatusCode();
        (await CreateRuleAsync(adminClient, resourceId, EffectType.SellBlocked, startsAt: upcomingStart,
            conditionOperator: ConditionOperator.LessThan, conditionValue: 100m)).EnsureSuccessStatusCode();

        var snapshot = await GetSnapshotAsync(playerClient);

        var rules = snapshot.Resources.Single(resource => resource.Id == resourceId).Rules;
        Assert.Equal(2, rules.Count);

        // Triées par date de début : la règle active d'abord, la règle à venir ensuite
        var activeRule = rules[0];
        Assert.Equal(EffectType.BuyBlocked, activeRule.Effect.Type);
        Assert.Equal(ConditionOperator.GreaterThan, activeRule.Condition.Operator);
        Assert.True(activeRule.StartsAt <= DateTime.UtcNow);

        var upcomingRule = rules[1];
        Assert.Equal(EffectType.SellBlocked, upcomingRule.Effect.Type);
        Assert.Equal(ConditionOperator.LessThan, upcomingRule.Condition.Operator);
        Assert.Equal(100m, upcomingRule.Condition.Value);
        Assert.True(upcomingRule.StartsAt > DateTime.UtcNow);  // pas encore active, mais déjà connue du téléphone
    }

    [Fact]
    public async Task PushResponse_ContainsTheSameStateAsAPull()
    {
        var playerClient = await _api.CreatePlayerClientAsync();
        var offlineBuy = new OfflineAction(Guid.NewGuid().ToString(), Carrot, TransactionType.Buy, 2, _api.SeedTime);

        var push = await ReadAsync<SyncPushResponse>(await PostSyncPushAsync(playerClient, offlineBuy));
        var pull = await GetSnapshotAsync(playerClient);

        // Le mobile peut remplacer son cache avec la réponse du push, sans refaire de GET
        Assert.Equal(push.NewBalance, push.Snapshot.Player.Balance);
        Assert.Equal(pull.Player.Balance, push.Snapshot.Player.Balance);
        Assert.Equal(
            pull.Player.Transactions.Select(transaction => transaction.TransactionId),
            push.Snapshot.Player.Transactions.Select(transaction => transaction.TransactionId));
        Assert.Equal(
            pull.Player.Portfolio.Sum(holding => holding.Lots.Sum(lot => lot.Quantity)),
            push.Snapshot.Player.Portfolio.Sum(holding => holding.Lots.Sum(lot => lot.Quantity)));
    }

    [Fact]
    public async Task Snapshot_AsAdmin_Returns403()
    {
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/sync");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Helpers ---

    private static async Task<GameSnapshot> GetSnapshotAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<GameSnapshot>("/api/sync", TestApiFactory.JsonOptions))!;
}