using MongoDB.Driver;
using TMS2000.Core.Models;
using TMS2000.Core.Data;

namespace TMS2000.Core.Repositories;

public class UserRepository
{
    private readonly IMongoCollection<User> _users; // side note => private + readonly pour encapsulé ma collection.

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>(MongoCollections.Users);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }
}
