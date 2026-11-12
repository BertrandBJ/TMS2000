using CommunityToolkit.Mvvm.ComponentModel;

namespace TMS2000.Mobile.ViewModels;

public partial class MarketRow : ObservableObject
{
    public MarketRow(string resourceId)
    {
        ResourceId = resourceId;
    }

    public string ResourceId { get; }

    [ObservableProperty] private string _resourceName = string.Empty;

    [ObservableProperty] private decimal _currentPrice;

    [ObservableProperty] private string _priceModeDescription = string.Empty;

    [ObservableProperty] private string _ownedDescription = string.Empty;

    [ObservableProperty] private string _rulesDescription = string.Empty;

    [ObservableProperty] private bool _hasRules;

    [ObservableProperty] private bool _isOwned;
}
