using TMS2000.Mobile.API;
using TMS2000.Mobile.Helpers;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Shared.DTO.Trades;

namespace TMS2000.Mobile.Services;

public class TradeService
{
    public const int MaxQuantityPerTrade = 1_000_000;

    private const string TradesRoute = "api/trades";
    private readonly ApiClient _apiClient;
    private readonly SnapshotService _snapshotService;

    public TradeService(ApiClient apiClient, SnapshotService snapshotService)
    {
        _apiClient = apiClient;
        _snapshotService = snapshotService;
    }

    public async Task<TradeResponse> ExecuteTradeAsync(string resourceId, TransactionType transactionType, int quantity)
    {
        var tradeRequest = new TradeRequest(Guid.NewGuid().ToString(), resourceId, transactionType, quantity);
        TradeResponse tradeResponse = await _apiClient.PostAsync<TradeRequest, TradeResponse>(TradesRoute, tradeRequest);

        await RefreshSnapshotAfterTradeAsync();
        return tradeResponse;
    }

    public async Task<int> ComputeMaxBuyQuantityAsync(string resourceId)
    {
        GameSnapshot freshSnapshot = await _snapshotService.RefreshAsync();

        SnapshotResourceDto? resource = freshSnapshot.Resources.FirstOrDefault(candidate => candidate.Id == resourceId);
        if (resource is null)
        {
            return 0;
        }

        decimal taxRate = RuleEvaluator.TaxRate(resource.Rules, freshSnapshot.ServerTime, resource.CurrentPrice);
        decimal unitCostWithTax = resource.CurrentPrice * (1 + taxRate);
        if (unitCostWithTax <= 0)
        {
            return 0;
        }

        int affordableQuantity = (int)Math.Min(
            Math.Floor(freshSnapshot.Player.Balance / unitCostWithTax),
            MaxQuantityPerTrade);

        int? quantityLimit = RuleEvaluator.MaxQuantity(resource.Rules, freshSnapshot.ServerTime, resource.CurrentPrice);
        return quantityLimit is int limit ? Math.Min(affordableQuantity, limit) : affordableQuantity;
    }

    private async Task RefreshSnapshotAfterTradeAsync()
    {
        try
        {
            await _snapshotService.RefreshAsync();
        }
        catch (Exception)
        {
        }
    }
}
