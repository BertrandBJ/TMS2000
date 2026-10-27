using System.Net;
using System.Net.Http.Json;
using TMS2000.Core;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Admin;
using TMS2000.Tests.Infrastructure;
using Xunit;
using static TMS2000.Tests.Infrastructure.ApiCalls;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AdminUsersTests
{
    private const string Carrot = "r-carrot";
    private const decimal CarrotPrice = 25m;   // prix actuel de r-carrot dans les données de test

    private readonly TestApiFactory _api;

    public AdminUsersTests(TestApiFactory api) => _api = api;

    [Fact]
    public async Task Users_ListsPlayers_ButNotAdmins()
    {
        var (playerId, _) = await CreatePlayerAsync();
        var adminClient = await _api.CreateAdminClientAsync();

        var users = (await adminClient.GetFromJsonAsync<UsersResponse>("/api/admin/users", TestApiFactory.JsonOptions))!;

        var listedPlayer = users.Users.Single(user => user.Id == playerId);
        Assert.Equal(TestApiFactory.PlayerStartingBalance, listedPlayer.Balance);
        Assert.DoesNotContain(users.Users, user => user.Email == TestApiFactory.AdminEmail);
    }

    [Fact]
    public async Task UserDetail_ShowsRemainingLots_AndMostRecentTransactionFirst()
    {
        var (playerId, playerClient) = await CreatePlayerAsync();
        await PostTradeAsync(playerClient, "buy-1", Carrot, TransactionType.Buy, 4);
        await PostTradeAsync(playerClient, "sell-1", Carrot, TransactionType.Sell, 1);
        var adminClient = await _api.CreateAdminClientAsync();

        var detail = (await adminClient.GetFromJsonAsync<UserDetailResponse>(
            $"/api/admin/users/{playerId}", TestApiFactory.JsonOptions))!;

        Assert.Equal(Roles.Player, detail.Role);
        var carrotHolding = Assert.Single(detail.Portfolio);
        var remainingLot = Assert.Single(carrotHolding.Lots);
        Assert.Equal(3, remainingLot.Quantity);            // 4 achetées, 1 vendue
        Assert.Equal(CarrotPrice, remainingLot.BuyPrice);
        Assert.Equal(new[] { "sell-1", "buy-1" },          // la plus récente en premier
            detail.Transactions.Select(transaction => transaction.TransactionId));
    }

    [Fact]
    public async Task UserDetail_WithUnknownId_Returns404()
    {
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await adminClient.GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/users/any-id")]
    public async Task AdminUsersRoutes_AsPlayer_Return403(string url)
    {
        var (_, playerClient) = await CreatePlayerAsync();

        var response = await playerClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Helpers ---

    // Comme CreatePlayerClientAsync, mais renvoie aussi l'id du joueur, nécessaire pour /api/admin/users/{id}
    private async Task<(string PlayerId, HttpClient PlayerClient)> CreatePlayerAsync()
    {
        var uniqueEmail = $"player-{Guid.NewGuid():N}@test.local";
        var playerId = await _api.InsertUserAsync(uniqueEmail, Roles.Player, TestApiFactory.PlayerStartingBalance);
        return (playerId, await _api.CreateAuthenticatedClientAsync(uniqueEmail));
    }
}