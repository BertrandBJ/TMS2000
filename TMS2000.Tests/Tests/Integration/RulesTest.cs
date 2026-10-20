using System.Net;
using System.Net.Http.Json;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Admin;
using TMS2000.Shared.DTO.Market;
using TMS2000.Shared.DTO.Trades;
using TMS2000.Tests.Infrastructure;
using static TMS2000.Tests.Infrastructure.ApiCalls;
// ici => call specifiquement les methodes static qui sont dans apiCalls, pas juste le namespace dispo.
using Xunit;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class RuleTests
{
    private const decimal TestResourcePrice = 50m;

    private readonly TestApiFactory _api;

    public RuleTests(TestApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task BuyBlockedRule_IsVisibleInMarket_AndRejectsPurchase()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        var playerClient = await _api.CreatePlayerClientAsync();

        var createResponse = await CreateRuleAsync(adminClient, resourceId, EffectType.BuyBlocked);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var marketResource = (await playerClient.GetFromJsonAsync<MarketResourceDto>($"/api/market/{resourceId}", TestApiFactory.JsonOptions))!;
        Assert.False(marketResource.CanBuy);
        Assert.Single(marketResource.ActiveRules);
        var purchaseResponse = await PostTradeAsync(playerClient, "buy-1", resourceId, TransactionType.Buy, 1);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, purchaseResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteRule_LiftsTheBlock_AndSecondDeleteReturns404()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        var playerClient = await _api.CreatePlayerClientAsync();
        var createdRule = await ReadAsync<CreateRuleResponse>(await CreateRuleAsync(adminClient, resourceId, EffectType.BuyBlocked));

        var deleteResponse = await adminClient.DeleteAsync($"/api/admin/rules/{createdRule.RuleId}");
        var secondDeleteResponse = await adminClient.DeleteAsync($"/api/admin/rules/{createdRule.RuleId}");
        var purchaseResponse = await PostTradeAsync(playerClient, "buy-1", resourceId, TransactionType.Buy, 1);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, purchaseResponse.StatusCode);
    }

    [Fact]
    public async Task CreateRule_WithSameEffectOnOverlappingPeriod_Returns409()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        await CreateRuleAsync(adminClient, resourceId, EffectType.BuyBlocked);

        var duplicateResponse = await CreateRuleAsync(adminClient, resourceId, EffectType.BuyBlocked);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task CreateRule_BeyondMaximumPerResource_Returns422()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        await CreateRuleAsync(adminClient, resourceId, EffectType.BuyBlocked);
        await CreateRuleAsync(adminClient, resourceId, EffectType.SellBlocked);
        await CreateRuleAsync(adminClient, resourceId, EffectType.QuantityLimit, effectValue: 5);

        var fourthRuleResponse = await CreateRuleAsync(adminClient, resourceId, EffectType.TransactionTax, effectValue: 10);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, fourthRuleResponse.StatusCode);
    }

    [Theory]
    [InlineData(EffectType.TransactionTax, 1, 0)]   // taxe sans valeur
    [InlineData(EffectType.BuyBlocked, 0, 0)]       // durée hors bornes
    [InlineData(EffectType.BuyBlocked, 1, -60)]     // début il y a une heure : dans le passé
    public async Task CreateRule_WithInvalidValues_Returns400(EffectType effectType, int durationHours, int startOffsetMinutes)
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        DateTime? startsAt = startOffsetMinutes == 0 ? null : DateTime.UtcNow.AddMinutes(startOffsetMinutes);

        var response = await CreateRuleAsync(adminClient, resourceId, effectType, durationHours: durationHours, startsAt: startsAt);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_OnUnknownResource_Returns404()
    {
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await CreateRuleAsync(adminClient, "r-does-not-exist", EffectType.BuyBlocked);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_AsPlayer_Returns403()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await CreateRuleAsync(playerClient, resourceId, EffectType.BuyBlocked);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TransactionTaxRule_IncreasesPurchaseCost()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        var playerClient = await _api.CreatePlayerClientAsync();
        await CreateRuleAsync(adminClient, resourceId, EffectType.TransactionTax, effectValue: 10);

        var purchase = await ReadAsync<TradeResponse>(await PostTradeAsync(playerClient, "buy-1", resourceId, TransactionType.Buy, 2));

        // 2 x 50 = 100, + 10 % de taxe = 110
        Assert.Equal(TestApiFactory.PlayerStartingBalance - 110m, purchase.NewBalance);
    }

}