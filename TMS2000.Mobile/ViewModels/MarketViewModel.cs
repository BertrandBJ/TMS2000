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

    public MarketViewModel(SnapshotStore snapshotStore, SnapshotService snapshotService, TokenStore tokenStore)
    {
        _snapshotStore = snapshotStore;
        _snapshotService = snapshotService;
        _tokenStore = tokenStore;
    }

    public ObservableCollection<MarketRow> MarketRows { get; } = new();

    [ObservableProperty] private decimal _balance;

    [ObservableProperty] private decimal _holdingsValue;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefreshing;

    // wrapped dans des methods qu'on va call quand la page pop/depop. 

    public void StartListening()
    {
        _snapshotStore.SnapshotUpdated += OnSnapshotUpdated;
        _tokenStore.SessionCleared += OnSessionCleared;

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
            await _snapshotService.RefreshAsync();
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
            IsRefreshing = false;
        }
    }
    private void OnSnapshotUpdated(object? sender, EventArgs eventArgs)
    {
        if (_snapshotStore.CurrentSnapshot is not null)
        {
            ShowSnapshot(_snapshotStore.CurrentSnapshot);
        }
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
            : $"Possédé : {ownedQuantity} (valeur {ownedQuantity * resource.CurrentPrice:N0} $)";

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