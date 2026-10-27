using System.Net;
using System.Net.Http.Json;
using TMS2000.Shared.DTO.Market;
using TMS2000.Tests.Infrastructure;
using Xunit;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class MarketTests
{
    // Historique de r-carrot posé par la fixture : 20 à SeedTime-2h, 30 à SeedTime-1h, 25 à SeedTime.
    // Aucun test ne pose de règle ni ne change son prix : ses valeurs sont stables.


    private const string Carrot = "r-carrot";
    private const string UnknownResource = "r-does-not-exist";

    private readonly TestApiFactory _api;

    public MarketTests(TestApiFactory api) => _api = api;

    [Fact]
    public async Task Market_ListsResources_WithVariationFromPreviousPrice()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var market = (await playerClient.GetFromJsonAsync<MarketResponse>("/api/market", TestApiFactory.JsonOptions))!;

        var carrot = market.Resources.Single(resource => resource.Id == Carrot);
        Assert.Equal(25m, carrot.CurrentPrice);
        Assert.Equal(-16.67m, carrot.Variation);              // (25 - 30) / 30 = -16,67 %
        Assert.True(carrot.CanBuy);
        Assert.Empty(carrot.ActiveRules);
        Assert.Contains(market.Resources, resource => resource.Id == "r-gold");
        Assert.True(market.NextTickAt > DateTime.UtcNow);     // le prochain tick est annoncé dans le futur
    }

    [Fact]
    public async Task Market_IsReadableByAdmin()
    {
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/market");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // la route est ouverte aux deux rôles
    }

    [Fact]
    public async Task History_ReturnsOnlyPointsWithinRequestedHours()
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var lastThreeHours = await GetHistoryAsync(playerClient, Carrot, hours: 3);
        var lastHour = await GetHistoryAsync(playerClient, Carrot, hours: 1);

        // Les points sont renvoyés du plus ancien au plus récent
        Assert.Equal(new[] { 20m, 30m, 25m }, lastThreeHours.Points.Select(point => point.Price));

        // Le point de SeedTime-1h tombe juste avant la limite "maintenant - 1h" : seul le dernier prix reste
        Assert.Equal(new[] { 25m }, lastHour.Points.Select(point => point.Price));
    }

    [Theory]
    [InlineData(0)]     // sous le minimum
    [InlineData(169)]   // au-dessus du maximum (168 h = 7 jours)
    public async Task History_WithHoursOutOfRange_Returns400(int hours)
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await playerClient.GetAsync($"/api/market/{Carrot}/history?hours={hours}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData($"/api/market/{UnknownResource}")]
    [InlineData($"/api/market/{UnknownResource}/history")]
    public async Task UnknownResource_Returns404(string url)
    {
        var playerClient = await _api.CreatePlayerClientAsync();

        var response = await playerClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Helpers ---

    private static async Task<PriceHistoryResponse> GetHistoryAsync(HttpClient client, string resourceId, int hours) =>
        (await client.GetFromJsonAsync<PriceHistoryResponse>(
            $"/api/market/{resourceId}/history?hours={hours}", TestApiFactory.JsonOptions))!;
}