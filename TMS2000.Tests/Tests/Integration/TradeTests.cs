using System.Net;
using System.Net.Http.Json;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Players;
using TMS2000.Shared.DTO.Trades;
using TMS2000.Tests.Infrastructure;
using static TMS2000.Tests.Infrastructure.ApiCalls;
using Xunit;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class TradeTests
{
    private const decimal CarrotPrice = 25m; // prix actuel de r-carrot dans les données de test

    private readonly TestApiFactory _api;

    public TradeTests(TestApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task Buy_WithEnoughBalance_DebitsBalanceAtCurrentPrice()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await PostTradeAsync(playerClient, "buy-1", "r-carrot", TransactionType.Buy, 4);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var trade = await ReadAsync<TradeResponse>(response);
        Assert.Equal(TransactionStatus.Accepted, trade.Status);
        Assert.Equal(CarrotPrice, trade.ExecutedPrice);
        Assert.Equal(TestApiFactory.PlayerStartingBalance - 4 * CarrotPrice, trade.NewBalance); // 1000 - 100 = 900
    }

    [Fact]
    public async Task Buy_SameTransactionIdTwice_IsExecutedOnlyOnce()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var firstResponse =
            await ReadAsync<TradeResponse>(await PostTradeAsync(playerClient, "buy-1", "r-carrot", TransactionType.Buy,
                4));
        var secondResponse =
            await ReadAsync<TradeResponse>(await PostTradeAsync(playerClient, "buy-1", "r-carrot", TransactionType.Buy,
                4));

        Assert.Equal(firstResponse.NewBalance, secondResponse.NewBalance); // pas de second débit
        var profile = await GetProfileAsync(playerClient);
        Assert.Equal(4, profile.Portfolio.Single().Quantity); // un seul achat de 4 en portefeuille
    }

    [Fact]
    public async Task BuyThenSell_AtSamePrice_RestoresBalanceWithZeroProfit()
    {
        var playerClient = await _api.CreatePlayerClientAsync();
        await PostTradeAsync(playerClient, "buy-1", "r-carrot", TransactionType.Buy, 4);

        var sellResponse = await PostTradeAsync(playerClient, "sell-1", "r-carrot", TransactionType.Sell, 4);

        var sale = await ReadAsync<TradeResponse>(sellResponse);
        Assert.Equal(0m, sale.Profit);
        Assert.Null(sale.Efficiency); // le prix n'a jamais dépassé le prix d'achat : aucun gain n'était possible
        Assert.Equal(TestApiFactory.PlayerStartingBalance, sale.NewBalance);
        var profile = await GetProfileAsync(playerClient);
        Assert.Empty(profile.Portfolio); // plus aucune carotte : la ligne du portefeuille a disparu
    }

    [Fact]
    public async Task Buy_WithoutEnoughBalance_Returns422()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response =
            await PostTradeAsync(playerClient, "buy-gold", "r-gold", TransactionType.Buy, 4); // 4 x 300 = 1200 > 1000

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Sell_WithoutHolding_Returns422()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await PostTradeAsync(playerClient, "sell-1", "r-carrot", TransactionType.Sell, 1);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Trade_OnUnknownResource_Returns404()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await PostTradeAsync(playerClient, "buy-1", "r-unknown", TransactionType.Buy, 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Trade_WithZeroQuantity_Returns400()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await PostTradeAsync(playerClient, "buy-1", "r-carrot", TransactionType.Buy, 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterBuy_ShowsHoldingWithAverageCost()
    {
        var playerClient = await _api.CreatePlayerClientAsync();
        await PostTradeAsync(playerClient, "buy-1", "r-carrot", TransactionType.Buy, 4);

        var profile = await GetProfileAsync(playerClient);

        var carrotHolding =
            Assert.Single(profile
                .Portfolio); // exactement une ligne, et on la récupère, si y'a 2 ou 0 portfolio ça rate.
        Assert.Equal("r-carrot", carrotHolding.ResourceId);
        Assert.Equal(4, carrotHolding.Quantity);
        Assert.Equal(CarrotPrice, carrotHolding.AverageCost);
        Assert.Equal(TestApiFactory.PlayerStartingBalance - 4 * CarrotPrice, profile.Balance);
    }
}

// --- Helpers ---, les truc qu'il faut normalement écrire à chaque test mais qui sont toujours les mêmes, c'est un peu des mini fonction locale pour l'écriture des test//

