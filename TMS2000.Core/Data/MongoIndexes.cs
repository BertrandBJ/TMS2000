using MongoDB.Driver;
using TMS2000.Core.Models;

namespace TMS2000.Core.Data;

public static class MongoIndexes
{
    public static async Task EnsureAllAsync(IMongoDatabase database)
    {
        var users = database.GetCollection<User>(MongoCollections.Users);

        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(

            Builders<User>.IndexKeys.Ascending(u => u.Email), // user email odre croissant

            new CreateIndexOptions { Unique = true })); // only 1 user per email en gros

        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(

            Builders<User>.IndexKeys.Ascending("refreshTokens.tokenHash"),

            new CreateIndexOptions { Name = "ix_users_refreshTokens_tokenHash" }));
    }
}