namespace TMS2000.Core.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); //Guid globally unique identifier, ça create une chaine aléatoire  => tostring() parce mongoDB
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin" ou "Player"
    
    public decimal Balance { get; set; }
    
    public List<Holding> Portfolio { get; set; } = new(); // whatever shit you have
}

public class Holding
{
    public string ResourceId { get; set; } = string.Empty; // du coup là s'est un call vers la collection ressources ?
    
    public string ResourceName { get; set; } = string.Empty; // whatever name the item is.
    
    public List<Lot> Lots { get; set; } = new(); // dedans on va mettre la quantité/le price d'achat 
}

public class Lot // correspond à un achat en gros.
{
    public int Quantity { get; set; } 
    
    public decimal BuyPrice { get; set; } 
    
    public DateTime BoughtAt { get; set; }
}