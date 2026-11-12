using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.API;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Market;
using TMS2000.Shared.DTO.Players;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Shared.DTO.Trades;

namespace TMS2000.Mobile.ViewModels;

public partial class ResourceDetailViewModel : ObservableObject, IQueryAttributable
{
    public const string ResourceIdQueryKey = "resourceId";

    private const int RecentResourceTransactionCount = 10;

    private readonly SnapshotStore _snapshotStore;
    private readonly TokenStore _tokenStore;
    private readonly TradeService _tradeService;
    private string? _resourceId;
    private SnapshotResourceDto? _currentResource;
    private DateTime _currentServerTime;

    public ResourceDetailViewModel(SnapshotStore snapshotStore, TokenStore tokenStore, TradeService tradeService)
    {
        _snapshotStore = snapshotStore;
        _tokenStore = tokenStore;
        _tradeService = tradeService;
    }

    public ObservableCollection<TransactionRow> ResourceTransactionRows { get; } = new();

    [ObservableProperty] private bool _hasNoResourceTransactions = true;

    [ObservableProperty] private string _resourceName = string.Empty;

    [ObservableProperty] private decimal _currentPrice;

    [ObservableProperty] private string _priceModeDescription = string.Empty;

    [ObservableProperty] private List<PricePointDto> _pricePoints = new();

    [ObservableProperty] private string _rulesDescription = string.Empty;

    [ObservableProperty] private decimal _balance;

    [ObservableProperty] private int _ownedQuantity;

    [ObservableProperty] private string _quantityText = "1";

    [ObservableProperty] private string? _resultMessage;

    [ObservableProperty] private bool _isLoss;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isBusy;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ResourceIdQueryKey, out object? resourceIdValue))
        {
            _resourceId = resourceIdValue as string;
            ShowCurrentSnapshot();
        }
    }

    public void StartListening()
    {
        _snapshotStore.SnapshotUpdated += OnSnapshotUpdated;
        _tokenStore.SessionCleared += OnSessionCleared;
        ShowCurrentSnapshot();
    }

    public void StopListening()
    {
        _snapshotStore.SnapshotUpdated -= OnSnapshotUpdated;
        _tokenStore.SessionCleared -= OnSessionCleared;
    }

    [RelayCommand]
    private Task BuyAsync() => TradeEnteredQuantityAsync(TransactionType.Buy);

    [RelayCommand]
    private Task SellAsync() => TradeEnteredQuantityAsync(TransactionType.Sell);

    [RelayCommand]
    private Task BuyMaxAsync() => RunTradeOperationAsync(async resourceId =>
    {
        int maxBuyQuantity = await _tradeService.ComputeMaxBuyQuantityAsync(resourceId);
        if (maxBuyQuantity <= 0)
        {
            ErrorMessage = "Solde insuffisant pour acheter une unité.";
            return;
        }

        await ExecuteTradeAsync(resourceId, TransactionType.Buy, maxBuyQuantity);
    });

    [RelayCommand]
    private Task SellAllAsync()
    {
        int maxSellQuantity = ApplyQuantityLimit(OwnedQuantity);

        if (maxSellQuantity <= 0)
        {
            ShowValidationError($"Vous ne possédez pas de {ResourceName}.");
            return Task.CompletedTask;
        }

        return TradeAsync(TransactionType.Sell, maxSellQuantity);
    }

    private Task TradeEnteredQuantityAsync(TransactionType transactionType)
    {
        if (!int.TryParse(QuantityText, out int quantity) || quantity <= 0)
        {
            ShowValidationError("Quantité invalide.");
            return Task.CompletedTask;
        }

        return TradeAsync(transactionType, quantity);
    }

    private int ApplyQuantityLimit(int wantedQuantity)
    {
        int cappedQuantity = Math.Min(wantedQuantity, TradeService.MaxQuantityPerTrade);
        if (_currentResource is null)
        {
            return cappedQuantity;
        }

        int? quantityLimit = RuleEvaluator.MaxQuantity(_currentResource.Rules, _currentServerTime, CurrentPrice);
        return quantityLimit is int limit ? Math.Min(cappedQuantity, limit) : cappedQuantity;
    }

    private void ShowValidationError(string message)
    {
        ResultMessage = null;
        IsLoss = false;
        ErrorMessage = message;
    }

    private Task TradeAsync(TransactionType transactionType, int quantity) =>
        RunTradeOperationAsync(resourceId => ExecuteTradeAsync(resourceId, transactionType, quantity));

    private async Task ExecuteTradeAsync(string resourceId, TransactionType transactionType, int quantity)
    {
        TradeResponse tradeResponse = await _tradeService.ExecuteTradeAsync(resourceId, transactionType, quantity);
        ResultMessage = DescribeTrade(transactionType, quantity, tradeResponse);
        IsLoss = transactionType == TransactionType.Sell && tradeResponse.Profit < 0;
    }

    private async Task RunTradeOperationAsync(Func<string, Task> tradeOperation)
    {
        if (IsBusy || _resourceId is null)
        {
            return;
        }

        ResultMessage = null;
        IsLoss = false;
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            await tradeOperation(_resourceId);
        }
        catch (ApiException apiException)
        {
            ErrorMessage = apiException.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Serveur injoignable.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Le serveur met trop de temps à répondre.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string DescribeTrade(TransactionType transactionType, int quantity, TradeResponse tradeResponse)
    {
        if (transactionType == TransactionType.Buy)
        {
            return $"Achat de {quantity} à {tradeResponse.ExecutedPrice:N0} $. Solde : {tradeResponse.NewBalance:N0} $";
        }

        decimal profit = tradeResponse.Profit ?? 0;
        string profitDescription = profit < 0
            ? $"Perte : {AmountFormatter.FormatSigned(profit)}"
            : $"Gain : {AmountFormatter.FormatSigned(profit)}";

        string saleDescription = $"Vente de {quantity} à {tradeResponse.ExecutedPrice:N0} $. {profitDescription}";
        if (tradeResponse.Efficiency is decimal efficiency)
        {
            saleDescription += $" (efficacité {efficiency:P0})";
        }

        return $"{saleDescription}. Solde : {tradeResponse.NewBalance:N0} $";
    }

    private void OnSnapshotUpdated(object? sender, EventArgs eventArgs)
    {
        ShowCurrentSnapshot();
    }

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        StopListening();
    }

    private void ShowCurrentSnapshot()
    {
        GameSnapshot? snapshot = _snapshotStore.CurrentSnapshot;
        if (snapshot is null || _resourceId is null)
        {
            return;
        }

        SnapshotResourceDto? resource = snapshot.Resources.FirstOrDefault(candidate => candidate.Id == _resourceId);
        if (resource is null)
        {
            return;
        }

        _currentResource = resource;
        _currentServerTime = snapshot.ServerTime;
        ResourceName = resource.Name;
        CurrentPrice = resource.CurrentPrice;
        PriceModeDescription = PriceModeDescriber.Describe(resource.Mode);
        PricePoints = resource.RecentPrices;
        RulesDescription = RuleDescriber.DescribeAll(resource.Rules, snapshot.ServerTime, resource.CurrentPrice);
        Balance = snapshot.Player.Balance;
        OwnedQuantity = snapshot.Player.Portfolio
            .Where(holding => holding.ResourceId == _resourceId)
            .SelectMany(holding => holding.Lots)
            .Sum(lot => lot.Quantity);

        ShowResourceTransactions(snapshot.Player.Transactions);
    }

    private void ShowResourceTransactions(List<TransactionDto> transactions)
    {
        List<TransactionDto> recentResourceTransactions = transactions
            .Where(transaction => transaction.ResourceId == _resourceId)
            .OrderByDescending(transaction => transaction.ExecutedAt ?? transaction.CreatedAt)
            .Take(RecentResourceTransactionCount)
            .ToList();

        CollectionSynchronizer.Synchronize(
            ResourceTransactionRows,
            recentResourceTransactions,
            (transactionRow, transaction) => transactionRow.TransactionId == transaction.TransactionId,
            transaction => new TransactionRow(transaction.TransactionId),
            (transactionRow, transaction) => transactionRow.UpdateFrom(transaction));

        HasNoResourceTransactions = recentResourceTransactions.Count == 0;
    }
}
