using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Players;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.ViewModels;

public partial class PortfolioViewModel : ObservableObject
{
    private readonly SnapshotStore _snapshotStore;
    private readonly TokenStore _tokenStore;

    public PortfolioViewModel(SnapshotStore snapshotStore, TokenStore tokenStore)
    {
        _snapshotStore = snapshotStore;
        _tokenStore = tokenStore;
    }

    public ObservableCollection<HoldingRow> HoldingRows { get; } = new();

    [ObservableProperty] private string _email = string.Empty;

    [ObservableProperty] private decimal _balance;

    [ObservableProperty] private string _realizedProfitDescription = string.Empty;

    [ObservableProperty] private Color _realizedProfitColor = Colors.Gray;

    [ObservableProperty] private string _averageEfficiencyDescription = string.Empty;

    [ObservableProperty] private Color _averageEfficiencyColor = Colors.Gray;

    [ObservableProperty] private string _tradeCountDescription = string.Empty;

    [ObservableProperty] private bool _hasNoHoldings = true;

    public void StartListening()
    {
        _snapshotStore.SnapshotUpdated += OnSnapshotUpdated;
        _tokenStore.SessionCleared += OnSessionCleared;

        if (_snapshotStore.CurrentSnapshot is not null)
        {
            ShowSnapshot(_snapshotStore.CurrentSnapshot);
        }
    }

    public void StopListening()
    {
        _snapshotStore.SnapshotUpdated -= OnSnapshotUpdated;
        _tokenStore.SessionCleared -= OnSessionCleared;
    }

    private void OnSnapshotUpdated(object? sender, EventArgs eventArgs)
    {
        if (_snapshotStore.CurrentSnapshot is not null)
        {
            ShowSnapshot(_snapshotStore.CurrentSnapshot);
        }
    }

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        StopListening();
    }

    private void ShowSnapshot(GameSnapshot snapshot)
    {
        SnapshotPlayerDto player = snapshot.Player;

        List<HoldingDto> orderedHoldings = player.Portfolio
            .Where(holding => holding.Lots.Count > 0)
            .OrderBy(holding => holding.ResourceName)
            .ToList();

        CollectionSynchronizer.Synchronize(
            HoldingRows,
            orderedHoldings,
            (holdingRow, holding) => holdingRow.ResourceId == holding.ResourceId,
            holding => new HoldingRow(holding.ResourceId),
            UpdateHoldingRow);

        HasNoHoldings = orderedHoldings.Count == 0;
        Email = player.Email;
        Balance = player.Balance;

        ShowTradeStatistics(player.Transactions);
    }

    private void ShowTradeStatistics(List<TransactionDto> transactions)
    {
        List<TransactionDto> acceptedTransactions = transactions
            .Where(transaction => transaction.Status == TransactionStatus.Accepted)
            .ToList();
        List<TransactionDto> acceptedSales = acceptedTransactions
            .Where(transaction => transaction.Type == TransactionType.Sell)
            .ToList();

        int purchaseCount = acceptedTransactions.Count - acceptedSales.Count;
        int rejectedCount = transactions.Count(transaction => transaction.Status == TransactionStatus.Rejected);

        decimal realizedProfit = acceptedSales.Sum(sale => sale.Profit ?? 0);
        RealizedProfitDescription = AmountFormatter.FormatSigned(realizedProfit);
        RealizedProfitColor = ResultColors.ForProfit(realizedProfit);

        List<decimal> knownEfficiencies = acceptedSales
            .Where(sale => sale.Efficiency.HasValue)
            .Select(sale => sale.Efficiency!.Value)
            .ToList();

        if (knownEfficiencies.Count == 0)
        {
            AverageEfficiencyDescription = "—";
            AverageEfficiencyColor = ResultColors.Neutral;
        }
        else
        {
            decimal averageEfficiency = knownEfficiencies.Average();
            AverageEfficiencyDescription = $"{averageEfficiency:P0} (sur {knownEfficiencies.Count} ventes)";
            AverageEfficiencyColor = ResultColors.ForEfficiency(averageEfficiency);
        }

        string tradeCountDescription = $"{purchaseCount} achats · {acceptedSales.Count} ventes";
        if (rejectedCount > 0)
        {
            tradeCountDescription += $" · {rejectedCount} refusées";
        }

        TradeCountDescription = tradeCountDescription;
    }

    private static void UpdateHoldingRow(HoldingRow holdingRow, HoldingDto holding)
    {
        holdingRow.ResourceName = holding.ResourceName;
        holdingRow.Quantity = holding.Lots.Sum(lot => lot.Quantity);
    }
}
