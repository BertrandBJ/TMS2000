using System.Net;
using System.Net.Http.Json;
using TMS2000.Shared.DTO.Admin;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Tests.Infrastructure;
using Xunit;
using static TMS2000.Tests.Infrastructure.ApiCalls;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AdminResourcesTests
{
    private const decimal TestResourcePrice = 10m;

    private readonly TestApiFactory _api;

    public AdminResourcesTests(TestApiFactory api) => _api = api;

    [Fact]
    public async Task UpdatePriceBand_WithOnlyBasePrice_UsesDefaultMultipliers()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await UpdatePriceBandAsync(adminClient, resourceId, basePrice: 10m);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedResource = await ReadAsync<AdminResourceDto>(response);
        Assert.Equal(10m, updatedResource.BasePrice);
        Assert.Equal(5m, updatedResource.MinPrice);
        Assert.Equal(30m, updatedResource.MaxPrice);
    }

    [Fact]
    public async Task UpdatePriceBand_WithExplicitBounds_IsListedForAdmin_AndSentInPlayerSnapshot()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();
        var playerClient = await _api.CreatePlayerClientAsync();

        await UpdatePriceBandAsync(adminClient, resourceId, basePrice: 10m, minPrice: 8m, maxPrice: 15m);

        var adminResources = (await adminClient.GetFromJsonAsync<AdminResourcesResponse>(
            "/api/admin/resources", TestApiFactory.JsonOptions))!;
        var listedResource = adminResources.Resources.Single(resource => resource.Id == resourceId);
        Assert.Equal((10m, 8m, 15m), (listedResource.BasePrice, listedResource.MinPrice, listedResource.MaxPrice));

        var snapshot = (await playerClient.GetFromJsonAsync<GameSnapshot>("/api/sync", TestApiFactory.JsonOptions))!;
        var snapshotResource = snapshot.Resources.Single(resource => resource.Id == resourceId);
        Assert.Equal((10m, 8m, 15m), (snapshotResource.BasePrice, snapshotResource.MinPrice, snapshotResource.MaxPrice));
    }

    [Theory]
    [InlineData(10, 12, 20)]
    [InlineData(10, 5, 8)]
    public async Task UpdatePriceBand_WithBaseOutsideBounds_Returns400(double basePrice, double minPrice, double maxPrice)
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await UpdatePriceBandAsync(adminClient, resourceId, (decimal)basePrice, (decimal)minPrice, (decimal)maxPrice);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePriceBand_WithUnknownResource_Returns404()
    {
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await UpdatePriceBandAsync(adminClient, $"r-unknown-{Guid.NewGuid():N}", basePrice: 10m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminResourcesRoutes_AsPlayer_Return403()
    {
        var resourceId = await _api.InsertResourceAsync(TestResourcePrice);
        var playerClient = await _api.CreatePlayerClientAsync();

        var listResponse = await playerClient.GetAsync("/api/admin/resources");
        var updateResponse = await UpdatePriceBandAsync(playerClient, resourceId, basePrice: 10m);

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}
