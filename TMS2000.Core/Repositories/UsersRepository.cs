using MongoDB.Driver;
using TMS2000.Core.Models;

namespace TMS2000.Core.Repositories;

public class UserRepository
{
    private readonly IMongoCollection<User> _users; // side note => private + readonly pour encapsulé ma collection.

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }
}