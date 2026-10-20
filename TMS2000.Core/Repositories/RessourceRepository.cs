using MongoDB.Driver;
using TMS2000.Core.Data;
using TMS2000.Core.Models;
using TMS2000.Shared;

namespace TMS2000.Core.Repositories;

public class ResourceRepository
{
    private readonly IMongoCollection<Resource> _resources;

    // Projections : on ne charge que ce dont on a besoin.
    // L'historique de prix peut contenir des milliers de points par ressource.
    private static readonly ProjectionDefinition<Resource> LatestTwoPrices =
        Builders<Resource>.Projection.Slice(r => r.PriceHistory, -2);

    private static readonly ProjectionDefinition<Resource> WithoutHistory =
        Builders<Resource>.Projection.Exclude(r => r.PriceHistory);

    public async Task<List<Resource>> GetByIdsAsync(IEnumerable<string> ids) => await _resources.Find(Builders<Resource>.Filter.In(r => r.Id, ids)).ToListAsync();

    public ResourceRepository(IMongoDatabase database)
    {
        _resources = database.GetCollection<Resource>(MongoCollections.Resources);
    }

    // Marché : toutes les ressources avec leurs 2 derniers prix (pour la variation)
    public async Task<List<Resource>> GetAllWithLatestPricesAsync() =>
        await _resources.Find(Builders<Resource>.Filter.Empty)
            .Project<Resource>(LatestTwoPrices)
            .ToListAsync();

    public async Task<Resource?> GetWithLatestPricesAsync(string id) =>
        await _resources.Find(r => r.Id == id)
            .Project<Resource>(LatestTwoPrices)
            .FirstOrDefaultAsync();

    // Document complet, historique inclus (graphique, calcul d'efficacité à la vente)
    public async Task<Resource?> GetByIdAsync(string id) =>
        await _resources.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<List<Resource>> GetAllWithoutHistoryAsync() =>
        await _resources.Find(Builders<Resource>.Filter.Empty)
            .Project<Resource>(WithoutHistory)
            .ToListAsync();

    public async Task<List<Resource>> GetByIdsWithoutHistoryAsync(IEnumerable<string> ids) =>
        await _resources.Find(Builders<Resource>.Filter.In(r => r.Id, ids))
            .Project<Resource>(WithoutHistory)
            .ToListAsync();

    // Tick : nouveau prix + nouveau point d'historique, en gardant les N derniers ($push + $slice)
    public async Task ApplyTickAsync(string id, decimal price, PriceMode mode, DateTime modeChangedAt, PricePoint point)
    {
        var update = Builders<Resource>.Update
            .Set(r => r.CurrentPrice, price)
            .Set(r => r.Mode, mode)
            .Set(r => r.ModeChangedAt, modeChangedAt)
            .PushEach(r => r.PriceHistory, new[] { point }, slice: -GameSettings.MaxPricePoints);

        await _resources.UpdateOneAsync(r => r.Id == id, update);
    }

    // Admin (étape 3) : la même règle est ajoutée dans chaque ressource ciblée
    public async Task AddRuleAsync(IEnumerable<string> ids, MarketRule rule) =>
        await _resources.UpdateManyAsync(
            Builders<Resource>.Filter.In(r => r.Id, ids),
            Builders<Resource>.Update.Push(r => r.Rules, rule));

    public async Task<bool> RemoveRuleAsync(string ruleId)
    {
        var result = await _resources.UpdateManyAsync(
            Builders<Resource>.Filter.ElemMatch(r => r.Rules, rule => rule.RuleId == ruleId),
            Builders<Resource>.Update.PullFilter(r => r.Rules, rule => rule.RuleId == ruleId));

        return result.ModifiedCount > 0;
    }
}