using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using TMS2000.Core;
using TMS2000.Core.Data;
using TMS2000.Core.Models;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Auth;
using Xunit;

namespace TMS2000.Tests.Infrastructure;

// Démarre la VRAIE API en mémoire, branchée sur une VRAIE MongoDB de test.
// Une seule instance partagée par toutes les classes de test (voir ApiCollection en bas).
// IasyncLifeTime => interface xunit qui s'assure que InitializeAsync est appelé une fois avant le 1er test.
public class TestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DatabaseName = "tms2000_integration_tests";
    public const string AdminEmail = "admin@test.local";
    public const string DefaultPassword = "Test-Password-123";
    public const decimal PlayerStartingBalance = 1000m;

    // Mêmes règles JSON que l'API : camelCase + enums en texte ("Buy", "Accepted"...), formatage en json similaire que le program prod.

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Instant de référence : l'historique de prix des données de test est construit autour

    public DateTime SeedTime { get; } = DateTime.UtcNow;

    private readonly IMongoDatabase _database;

    public TestApiFactory()
    {
        // En local : le Mongo de dctest. En CI : la variable pointera vers le service Mongo du job.

        var mongoConnectionString = Environment.GetEnvironmentVariable("TMS2000_TEST_MONGO")
                                    ?? "mongodb://localhost:27018";

        // La configuration de l'API passe par des variables d'environnement, exactement comme dans Docker.
        // Program.cs les lit au démarrage : elles doivent exister AVANT que l'API soit créée.

        Environment.SetEnvironmentVariable("ConnectionStrings__MongoDb", mongoConnectionString);
        Environment.SetEnvironmentVariable("MongoSettings__DatabaseName", DatabaseName);
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-tests-only-secret-key-at-least-32-bytes");
        Environment.SetEnvironmentVariable("Jwt__ExpiryHours", "1");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "30");
        Environment.SetEnvironmentVariable("PriceEngine__TickIntervalSeconds", "86400"); // aucun tick : les prix ne bougent pas

        MongoConventions.Register(); // mêmes conventions (camelCase, enums en string) que l'API
        _database = new MongoClient(mongoConnectionString).GetDatabase(DatabaseName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Test");

    // Appelée par xUnit UNE fois, avant le premier test : base vide + données de départ connues

    public async Task InitializeAsync()
    {
        await _database.Client.DropDatabaseAsync(DatabaseName);
        await SeedResourcesAsync();
        await InsertUserAsync(AdminEmail, Roles.Admin, 0m);
    }

    // Appelée par xUnit après le dernier test (rien à nettoyer : la base est vidée au prochain lancement)

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    // --- Données de départ ---

    private async Task SeedResourcesAsync()
    {
        var resources = _database.GetCollection<Resource>(MongoCollections.Resources);
        await resources.InsertManyAsync(new[]
        {
            CreateResource("r-carrot", "Carotte", new[] { 20m, 30m, 25m }),  // trade, synchro
            CreateResource("r-gold", "Or", new[] { 300m, 300m, 300m }),       // solde insuffisant
            CreateResource("r-iron", "Fer", new[] { 50m, 50m, 50m })          // réservée aux tests de règles
        });
    }

    // Un point d'historique par heure, le dernier à SeedTime ; le dernier prix est le prix actuel.
    // r-carrot : 20 à SeedTime-2h, 30 à SeedTime-1h, 25 à SeedTime.

    private Resource CreateResource(string id, string name, decimal[] hourlyPrices)
    {
        var lastIndex = hourlyPrices.Length - 1;
        var priceHistory = hourlyPrices
            .Select((price, index) => new PricePoint { Price = price, At = SeedTime.AddHours(index - lastIndex) })
            .ToList();

        return new Resource
        {
            Id = id,
            Name = name,
            CurrentPrice = hourlyPrices[lastIndex],
            Mode = PriceMode.Stable,
            ModeChangedAt = SeedTime,
            PriceHistory = priceHistory
        };
    }

    public async Task<string> InsertUserAsync(string email, string role, decimal balance)
    {
        var users = _database.GetCollection<User>(MongoCollections.Users);
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, workFactor: 4), // 4 = rapide, suffisant en test, de base c'est 11 mais là osef
            Role = role,
            Balance = balance
        };
        await users.InsertOneAsync(user);
        return user.Id;
    }

    // Ressource neuve à usage unique : les règles sont stockées DANS la ressource,
    // une ressource par test évite que les règles d'un test bloquent ou saturent un autre test.
    public async Task<string> InsertResourceAsync(decimal currentPrice)
    {
        // le :N enleve le " - " dans le nommage du guid. Why not ? 
        var uniqueResourceId = $"r-test-{Guid.NewGuid():N}";
        var resources = _database.GetCollection<Resource>(MongoCollections.Resources);
        await resources.InsertOneAsync(CreateResource(uniqueResourceId, "Ressource de test", new[] { currentPrice }));
        return uniqueResourceId;
    }

    // --- Clients HTTP prêts à l'emploi ---

    // Chaque test crée SON joueur : aucun test ne dépend de ce qu'un autre a acheté ou vendu

    public async Task<HttpClient> CreatePlayerClientAsync()
    {
        var uniqueEmail = $"player-{Guid.NewGuid():N}@test.local";
        await InsertUserAsync(uniqueEmail, Roles.Player, PlayerStartingBalance);
        return await CreateAuthenticatedClientAsync(uniqueEmail);
    }

    public Task<HttpClient> CreateAdminClientAsync() => CreateAuthenticatedClientAsync(AdminEmail);

    // Login par la vraie route, puis le JWT est mis dans le header Authorization du client

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DefaultPassword), JsonOptions);
        loginResponse.EnsureSuccessStatusCode();

        var tokens = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }
}

// Toutes les classes de test marquées [Collection(ApiCollection.Name)] partagent UNE seule TestApiFactory :
// une seule API démarrée, une seule base préparée, et les tests de la collection s'exécutent l'un après l'autre.
// c'est une écriture spécifique déclarative pour Xunit (sinon il va rebuild de zéro une api à chaque fois pour un test 
// c'est une collection fixture. Il exise une collection du nom API et tout ses membres la partage (TestApiFactory est son nom interne dans le code du coup).


[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<TestApiFactory>
{
    public const string Name = "Api";
}