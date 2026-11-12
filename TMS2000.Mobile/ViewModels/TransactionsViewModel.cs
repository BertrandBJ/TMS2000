using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Players;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.ViewModels;

public partial class TransactionsViewModel : ObservableObject
{
    private readonly SnapshotStore _snapshotStore;
    private readonly TokenStore _tokenStore;

    public TransactionsViewModel(SnapshotStore snapshotStore, TokenStore tokenStore)
    {
        _snapshotStore = snapshotStore;
        _tokenStore = tokenStore;
    }

    public ObservableCollection<TransactionRow> TransactionRows { get; } = new();

    [ObservableProperty] private bool _hasNoTransactions = true;

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
        List<TransactionDto> newestFirstTransactions = snapshot.Player.Transactions
            .OrderByDescending(transaction => transaction.ExecutedAt ?? transaction.CreatedAt)
            .ToList();

        CollectionSynchronizer.Synchronize(
            TransactionRows,
            newestFirstTransactions,
            (transactionRow, transaction) => transactionRow.TransactionId == transaction.TransactionId,
            transaction => new TransactionRow(transaction.TransactionId),
            (transactionRow, transaction) => transactionRow.UpdateFrom(transaction));

        HasNoTransactions = newestFirstTransactions.Count == 0;
    }
}
