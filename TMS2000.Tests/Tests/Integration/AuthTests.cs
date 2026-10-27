using System.Net;
using System.Net.Http.Json;
using TMS2000.Core;
using TMS2000.Shared.DTO.Auth;
using TMS2000.Tests.Infrastructure;
using TMS2000.Shared;

using Xunit;

namespace TMS2000.Tests.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests
{
    private readonly TestApiFactory _api;

    public AuthTests(TestApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        // Arrange : préparer
        var client = _api.CreateClient();
        var request = new LoginRequest(TestApiFactory.AdminEmail, TestApiFactory.DefaultPassword);

        // Act : exécuter l'action testée
        var response = await client.PostAsJsonAsync("/api/auth/login", request, TestApiFactory.JsonOptions);

        // Assert : vérifier le résultat
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<LoginResponse>(TestApiFactory.JsonOptions))!;
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Equal(Roles.Admin, tokens.Role);
    }

    [Theory]
    [InlineData(TestApiFactory.AdminEmail, "wrong-password")]       // mauvais mot de passe
    [InlineData("nobody@test.local", TestApiFactory.DefaultPassword)] // email inconnu
    public async Task Login_WithInvalidCredentials_Returns401(string email, string password)
    {
        var client = _api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), TestApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveOnEmail()
    {
        var client = _api.CreateClient();
        var upperCaseEmail = TestApiFactory.AdminEmail.ToUpperInvariant();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(upperCaseEmail, TestApiFactory.DefaultPassword), TestApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingPassword_Returns400()
    {
        var client = _api.CreateClient();

        // Objet anonyme : on envoie volontairement un body incomplet
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = TestApiFactory.AdminEmail });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ConsumesOldTokenAndReturnsNewPair()
    {
        var client = _api.CreateClient();
        var firstLogin = await LoginAsAdminAsync(client);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(firstLogin.RefreshToken), TestApiFactory.JsonOptions);
        var reuseOldTokenResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(firstLogin.RefreshToken), TestApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshedTokens = (await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>(TestApiFactory.JsonOptions))!;
        Assert.NotEqual(firstLogin.RefreshToken, refreshedTokens.RefreshToken);   // rotation : nouveau token
        Assert.Equal(HttpStatusCode.Unauthorized, reuseOldTokenResponse.StatusCode); // l'ancien est consommé
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var client = _api.CreateClient();
        var login = await LoginAsAdminAsync(client);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest(login.RefreshToken), TestApiFactory.JsonOptions);
        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken), TestApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithoutToken_Returns401()
    {
        var anonymousClient = _api.CreateClient();

        var response = await anonymousClient.GetAsync("/api/market");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlayerRoute_WithAdminToken_Returns403()
    {
        var adminClient = await _api.CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_BeyondMaxSessions_RevokesOldestRefreshToken()
    {
        var client = _api.CreateClient();
        var playerEmail = $"player-{Guid.NewGuid():N}@test.local";   // joueur dédié : l'admin est partagé par d'autres tests
        await _api.InsertUserAsync(
            playerEmail,
            Roles.Player,
            TestApiFactory.PlayerStartingBalance);

        // Une connexion de plus que le maximum de sessions : la plus ancienne doit être éjectée

        var refreshTokensFromOldestToNewest = new List<string>();
        for (var loginNumber = 0; loginNumber <= GameSettings.MaxSessionsPerUser; loginNumber++)
            refreshTokensFromOldestToNewest.Add((await LoginAsync(client, playerEmail)).RefreshToken);

        var oldestSessionRefresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(refreshTokensFromOldestToNewest[0]), TestApiFactory.JsonOptions);

        var secondOldestSessionRefresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(refreshTokensFromOldestToNewest[1]), TestApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, oldestSessionRefresh.StatusCode);   // éjectée
        Assert.Equal(HttpStatusCode.OK, secondOldestSessionRefresh.StatusCode);       // toujours valide
    }

    [Fact]
    public async Task Health_IsPublic_AndReportsHealthy()
    {
        var anonymousClient = _api.CreateClient();

        var response = await anonymousClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync()); // l'API ET MongoDB répondent
    }


    // call api helper//
    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, TestApiFactory.DefaultPassword), TestApiFactory.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(TestApiFactory.JsonOptions))!;
    }

    private static async Task<LoginResponse> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(TestApiFactory.AdminEmail, TestApiFactory.DefaultPassword), TestApiFactory.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(TestApiFactory.JsonOptions))!;
    }
}