namespace TMS2000.Core.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); //Guid globally unique identifier, ça create une chaine aléatoire  => tostring() parce mongoDB
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin" ou "Player"
}