using MongoDB.Driver;
using TMS2000.Core;
using TMS2000.Core.Data;
using TMS2000.Core.Models;
using TMS2000.Core.Helper;
using TMS2000.Shared;

MongoConventions.Register();

string Required(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"La variable d'environnement '{name}' est manquante ou vide.");
    return value;
}

var database = new MongoClient(Required("ConnectionStrings__MongoDb"))
    .GetDatabase(Required("MongoSettings__DatabaseName"));

await MongoIndexes.EnsureAllAsync(database);
// je devrai changer le nom de la methode, parce que c'est pas ouf descriptif (c'est pour email/user => croissant/unique)//

var resources = database.GetCollection<Resource>(MongoCollections.Resources);
var users = database.GetCollection<User>(MongoCollections.Users);

var now = DateTime.UtcNow;
var random = new Random(); // je suppose que ça va être pour le mode de growth/Degrowth.
var modes = Enum.GetValues<PriceMode>();

var seedResources = new (string Id, string Name, decimal Price)[]
{
    ("r-carrot", "Carotte", 1m),
    ("r-potato", "Pomme de terre", 2m),
    ("r-wheat", "Blé", 4m),
    ("r-wood", "Bois", 8m),
    ("r-coal", "Charbon", 15m),
    ("r-iron", "Fer", 30m),
    ("r-copper", "Cuivre", 60m),
    ("r-oil", "Pétrole", 120m),
    ("r-silver", "Argent", 250m),
    ("r-gold", "Or", 500m)
};

foreach (var (id, name, price) in seedResources) //evite les doublons//
{
    var priceBand = PriceBand.FromBasePrice(price);
    var existingResource = await resources.Find(r => r.Id == id).FirstOrDefaultAsync();

    if (existingResource is not null)
    {
        if (PriceBand.For(existingResource).IsDefined)
        {
            Console.WriteLine($"Ressource '{name}' déjà présente, ignorée."); // si un truc est déjà dans la bdd, on ignore et go next// normalement avec mangoDB qui fait _id auto donc déjà unique mais bon//
            continue;
        }

        var bandUpdate = Builders<Resource>.Update
            .Set(r => r.BasePrice, priceBand.BasePrice)
            .Set(r => r.MinPrice, priceBand.MinPrice)
            .Set(r => r.MaxPrice, priceBand.MaxPrice);

        if (!priceBand.Contains(existingResource.CurrentPrice))
            bandUpdate = bandUpdate.Set(r => r.CurrentPrice, priceBand.BasePrice);

        await resources.UpdateOneAsync(r => r.Id == id, bandUpdate);
        Console.WriteLine($"Ressource '{name}' : fourchette de prix ajoutée ({priceBand.MinPrice} - {priceBand.MaxPrice}).");
        continue;
    }

    await resources.InsertOneAsync(new Resource
    {
        Id = id,
        Name = name,
        CurrentPrice = price,
        BasePrice = priceBand.BasePrice,
        MinPrice = priceBand.MinPrice,
        MaxPrice = priceBand.MaxPrice,
        Mode = modes[random.Next(modes.Length)],
        ModeChangedAt = now,
        PriceHistory = new() { new PricePoint { Price = price, At = now } }
    });
    Console.WriteLine($"Ressource '{name}' créée.");
}

await SeedUserAsync(Required("Seed__AdminEmail"), Required("Seed__AdminPassword"), "Admin", 0m);
await SeedUserAsync(Required("Seed__PlayerEmail"), Required("Seed__PlayerPassword"), "Player", GameSettings.PlayerStartingBalance);

Console.WriteLine("Seed terminé.");

// below => là je fais 2 request à la suite, en théorie vu que mongoDB porte la charge de l'unicité je pourrai juste faire un try-catch. (et catch l'erreur mongoDB).

async Task SeedUserAsync(string email, string password, string role, decimal balance)
{
    var normalizedEmail = EmailNormalizer.Normalize(email);
    if (await users.Find(u => u.Email == normalizedEmail).AnyAsync())
    {
        Console.WriteLine($"Utilisateur '{normalizedEmail}' déjà présent, ignoré.");
        return;
    }

    await users.InsertOneAsync(new User
    {
        Email = normalizedEmail,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = role,
        Balance = balance
    });
    Console.WriteLine($"Utilisateur '{normalizedEmail}' ({role}) créé.");
}