using TMS2000.Shared;
namespace TMS2000.Core.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); //Guid globally unique identifier, ça create une chaine aléatoire  => tostring() parce mongoDB
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin" ou "Player"

    public decimal Balance { get; set; }

    public int Version { get; set; }

    public List<Holding> Portfolio { get; set; } = new(); // whatever shit you have

    public List<Transaction> Transactions { get; set; } = new(); // sans ça pas d'imbrication right.

    public List<RefreshToken> RefreshTokens { get; set; } = new();

}

public class Holding
{
    public string ResourceId { get; set; } = string.Empty; // 

    public string ResourceName { get; set; } = string.Empty; // whatever name the item is.

    public List<Lot> Lots { get; set; } = new(); // dedans on va mettre la quantité/le price d'achat 
}

public class Lot // correspond à un achat en gros.
{
    public int Quantity { get; set; }

    public decimal BuyPrice { get; set; }

    public DateTime BoughtAt { get; set; }
}

public class Transaction
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public decimal ExecutedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public bool Offline { get; set; }
    public TransactionStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public decimal? Profit { get; set; }
    public decimal? Efficiency { get; set; }
    public decimal? OfflineBonus { get; set; }
}

public class RefreshToken
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

}