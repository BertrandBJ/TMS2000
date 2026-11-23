using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.API;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.ViewModels;

// Onglet market du joueur : les ressources et leur prix actuel, data from snapshotstore

public partial class MarketViewModel : ObservableObject
{
    private readonly SnapshotStore _snapshotStore;
    private readonly SnapshotService _snapshotService;
    private readonly TokenStore _tokenStore;
    private readonly ConnectionStatus _connectionStatus;
    private readonly PendingActionQueue _pendingActionQueue;
    private readonly PendingActionSynchronizer _pendingActionSynchronizer;

    public MarketViewModel(
        SnapshotStore snapshotStore,
        SnapshotService snapshotService,
        TokenStore tokenStore,
        ConnectionStatus connectionStatus,
        PendingActionQueue pendingActionQueue,
        PendingActionSynchronizer pendingActionSynchronizer)
    {
        _snapshotStore = snapshotStore;
        _snapshotService = snapshotService;
        _tokenStore = tokenStore;
        _connectionStatus = connectionStatus;
        _pendingActionQueue = pendingActionQueue;
        _pendingActionSynchronizer = pendingActionSynchronizer;
    }

    public ObservableCollection<MarketRow> MarketRows { get; } = new();

    [ObservableProperty] private decimal _balance;

    [ObservableProperty] private decimal _holdingsValue;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefreshing;

    [ObservableProperty] private string? _connectionDescription;

    [ObservableProperty] private bool _hasConnectionDescription;

    [ObservableProperty] private string? _syncMessage;

    [ObservableProperty] private bool _hasSyncMessage;

    // wrapped dans des methods qu'on va call quand la page pop/depop. 

    public void StartListening()
    {
        _snapshotStore.SnapshotUpdated += OnSnapshotUpdated;
        _tokenStore.SessionCleared += OnSessionCleared;
        _connectionStatus.StatusChanged += OnConnectionStatusChanged;
        _pendingActionQueue.PendingActionsChanged += OnPendingActionsChanged;

        ShowConnectionStatus();

        if (_snapshotStore.CurrentSnapshot is null)
        {
            RefreshCommand.Execute(null);
            return;
        }
        ShowSnapshot(_snapshotStore.CurrentSnapshot);

    }
    public void StopListening()
    {
        _snapshotStore.SnapshotUpdated -= OnSnapshotUpdated;
        _tokenStore.SessionCleared -= OnSessionCleared;
        _connectionStatus.StatusChanged -= OnConnectionStatusChanged;
        _pendingActionQueue.PendingActionsChanged -= OnPendingActionsChanged;
    }

    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {
        StopListening();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        try
        {
            if (_pendingActionQueue.Count > 0)
            {
                await _pendingActionSynchronizer.SyncAsync();
            }

            await _snapshotService.RefreshAsync();
            _connectionStatus.MarkReachable();
        }
        catch (ApiException apiException)
        {
            ErrorMessage = apiException.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _connectionStatus.MarkUnreachable();
            ErrorMessage = _snapshotStore.CurrentSnapshot is null
                ? "Serveur injoignable et aucune donnée enregistrée sur ce téléphone."
                : null;
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    private void OnSnapshotUpdated(object? sender, EventArgs eventArgs)
    {
        if (_snapshotStore.CurrentSnapshot is not null)
        {
            ErrorMessage = null;
            ShowSnapshot(_snapshotStore.CurrentSnapshot);
        }

        ShowConnectionStatus();
    }

    private void OnConnectionStatusChanged(object? sender, EventArgs eventArgs)
    {
        ShowConnectionStatus();
    }

    private void OnPendingActionsChanged(object? sender, EventArgs eventArgs)
    {
        ShowConnectionStatus();
    }

    private void ShowConnectionStatus()
    {
        int pendingActionCount = _pendingActionQueue.Count;
        string pendingDescription = pendingActionCount switch
        {
            0 => string.Empty,
            1 => "1 ordre en attente",
            _ => $"{pendingActionCount} ordres en attente"
        };

        if (_connectionStatus.IsOffline)
        {
            List<string> descriptionParts = new() { "Hors ligne", DescribeDataAge() };
            if (pendingActionCount > 0)
            {
                descriptionParts.Add(pendingDescription);
            }

            ConnectionDescription = string.Join(" · ", descriptionParts);
        }
        else
        {
            ConnectionDescription = pendingActionCount > 0 ? $"{pendingDescription} d'envoi" : null;
        }

        HasConnectionDescription = ConnectionDescription is not null;
        SyncMessage = _connectionStatus.SyncMessage;
        HasSyncMessage = SyncMessage is not null;
    }

    private string DescribeDataAge()
    {
        if (_snapshotStore.ReceivedAt is not DateTime receivedAt)
        {
            return "aucune donnée enregistrée";
        }

        DateTime localReceivedAt = receivedAt.ToLocalTime();
        return localReceivedAt.Date == DateTime.Today
            ? $"données de {localReceivedAt:HH:mm}"
            : $"données du {localReceivedAt:dd/MM HH:mm}";
    }

    private void ShowSnapshot(GameSnapshot snapshot)
    {
        foreach (SnapshotResourceDto resource in snapshot.Resources)
        {
            MarketRow? marketRow = MarketRows.FirstOrDefault(row => row.ResourceId == resource.Id);
            if (marketRow is null)
            {
                marketRow = new MarketRow(resource.Id);
                MarketRows.Add(marketRow);
            }

            UpdateMarketRow(marketRow, resource, snapshot);
        }

        List<MarketRow> rowsWithoutResource = MarketRows
            .Where(row => snapshot.Resources.All(resource => resource.Id != row.ResourceId))
            .ToList();
        foreach (MarketRow rowWithoutResource in rowsWithoutResource)
        {
            MarketRows.Remove(rowWithoutResource);
        }

        Balance = snapshot.Player.Balance;
        HoldingsValue = snapshot.Resources.Sum(resource =>
            CountOwnedQuantity(snapshot, resource.Id) * resource.CurrentPrice);
    }

    private static void UpdateMarketRow(MarketRow marketRow, SnapshotResourceDto resource, GameSnapshot snapshot)
    {
        int ownedQuantity = CountOwnedQuantity(snapshot, resource.Id);

        string ownedDescription = ownedQuantity == 0
            ? "Possédé : aucun"
            : $"Possédé : {ownedQuantity} (valeur {AmountFormatter.FormatMoney(ownedQuantity * resource.CurrentPrice)})";

        marketRow.ResourceName = resource.Name;
        marketRow.CurrentPrice = resource.CurrentPrice;
        marketRow.PriceModeDescription = PriceModeDescriber.Describe(resource.Mode);
        marketRow.OwnedDescription = ownedDescription;
        marketRow.RulesDescription = RuleDescriber.DescribeEach(resource.Rules, snapshot.ServerTime, resource.CurrentPrice);
        marketRow.HasRules = resource.Rules.Count > 0;
        marketRow.IsOwned = ownedQuantity > 0;
    }

    private static int CountOwnedQuantity(GameSnapshot snapshot, string resourceId) =>
        snapshot.Player.Portfolio
            .Where(holding => holding.ResourceId == resourceId)
            .SelectMany(holding => holding.Lots)
            .Sum(lot => lot.Quantity);
}
