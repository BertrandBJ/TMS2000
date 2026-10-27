using MongoDB.Driver;
using TMS2000.Core.Data;
using TMS2000.Core.Models;
using TMS2000.Core.Helper;
using TMS2000.Shared;

MongoConventions.Register();

string Required(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"La variable d'environnement '{name}' est manquante.");

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
    ("r-carrot", "Carotte", 25m),
    ("r-potato", "Pomme de terre", 18m),
    ("r-wheat", "Blé", 12m),
    ("r-wood", "Bois", 30m),
    ("r-coal", "Charbon", 40m),
    ("r-iron", "Fer", 55m),
    ("r-copper", "Cuivre", 70m),
    ("r-oil", "Pétrole", 90m),
    ("r-silver", "Argent", 150m),
    ("r-gold", "Or", 300m)
};

foreach (var (id, name, price) in seedResources) //evite les doublons//
{
    if (await resources.Find(r => r.Id == id).AnyAsync())
    {
        Console.WriteLine($"Ressource '{name}' déjà présente, ignorée."); // si un truc est déjà dans la bdd, on ignore et go next// normalement avec mangoDB qui fait _id auto donc déjà unique mais bon//
        continue;
    }

    await resources.InsertOneAsync(new Resource
    {
        Id = id,
        Name = name,
        CurrentPrice = price,
        Mode = modes[random.Next(modes.Length)],
        ModeChangedAt = now,
        PriceHistory = new() { new PricePoint { Price = price, At = now } }
    });
    Console.WriteLine($"Ressource '{name}' créée.");
}

await SeedUserAsync(Required("Seed__AdminEmail"), Required("Seed__AdminPassword"), "Admin", 0m);
await SeedUserAsync(Required("Seed__PlayerEmail"), Required("Seed__PlayerPassword"), "Player", 1000m);

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