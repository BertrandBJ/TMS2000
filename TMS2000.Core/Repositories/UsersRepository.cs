using MongoDB.Driver;
using TMS2000.Core.Data;
using TMS2000.Core.Models;
using TMS2000.Core.Repositories;

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

        // synthax : if (result.MatchedCount == 1)
        //     return true;
        // else
        //     return false;
        // un opérateur ==1 return déjà false ou true en fonction de la situation, 
    }

    // --- Refresh tokens ---//



    // Login : ajoute le token, en ne gardant que les N plus récents

    // Below : en faite on incrémente de 1 la version à chaque action sur les tokens. Parce que les actions de trades re-writer tout le user dans mongo.
    // l'autre solution, c'est de faire des request plus plus ciblé (pour le trade)
    // encore mieux : sortir l'auth de user (dans les collections mongoDB

    public async Task AddRefreshTokenAsync(string userId, RefreshToken token)
    {

        // ajoute un refreshtoken, slice en fonction de MaxSessionPerUser => 5 ici, 5 stocked au max, remove le plus old//

        var update = Builders<User>.Update
            .PushEach(u => u.RefreshTokens, new[] { token }, slice: -GameSettings.MaxSessionsPerUser)
            .Inc(u => u.Version, 1);

        await _users.UpdateOneAsync(u => u.Id == userId, update);
    }

    // Refresh : remplace atomiquement le token valide par le nouveau, et renvoie le joueur.
    // null si le token est inconnu, expiré ou déjà utilisé.
    // note : on demande bien le oldTokenHASH. donc quand on va call la method il faut que ce soit le tokenHashed du user.
    public async Task<User?> RotateRefreshTokenAsync(string oldTokenHash, RefreshToken newToken, DateTime now)
    {
        var filter = Builders<User>.Filter.ElemMatch(
            u => u.RefreshTokens,
            t => t.TokenHash == oldTokenHash && t.ExpiresAt > now);

        var update = Builders<User>.Update
            .Set("refreshTokens.$", newToken)
            .Inc(u => u.Version, 1);

        return await _users.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After });
    }

    // Logout : supprime le token


    public async Task RemoveRefreshTokenAsync(string tokenHash) =>
        await _users.UpdateOneAsync(
            Builders<User>.Filter.ElemMatch(u => u.RefreshTokens, t => t.TokenHash == tokenHash),
            Builders<User>.Update
                .PullFilter(u => u.RefreshTokens, t => t.TokenHash == tokenHash)
                .Inc(u => u.Version, 1));
}

