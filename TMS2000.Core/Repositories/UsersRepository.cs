using MongoDB.Driver;
using TMS2000.Core.Data;
using TMS2000.Core.Models;

namespace TMS2000.Core.Repositories;

public class UserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>(MongoCollections.Users);
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

    public async Task<User?> GetByIdAsync(string id) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

   // liste des players pour l'admin below : 
    public async Task<List<User>> GetPlayersAsync()
    {
        var projection = Builders<User>.Projection
            .Exclude(u => u.PasswordHash)
            .Exclude(u => u.Portfolio)
            .Exclude(u => u.Transactions)
            .Exclude(u => u.RefreshTokens);

        return await _users.Find(u => u.Role == Roles.Player)
            .Project<User>(projection)
            .ToListAsync();
    }
    
    public async Task<bool> ReplaceIfVersionMatchesAsync(User user, int expectedVersion)
    
    {
        var result = await _users.ReplaceOneAsync(
            u => u.Id == user.Id && u.Version == expectedVersion,
            user);

        return result.MatchedCount == 1;
    }
}