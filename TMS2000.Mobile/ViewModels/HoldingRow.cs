using CommunityToolkit.Mvvm.ComponentModel;

namespace TMS2000.Mobile.ViewModels;

public partial class HoldingRow : ObservableObject
{
    public HoldingRow(string resourceId)
    {
        ResourceId = resourceId;
    }

    public string ResourceId { get; }

    [ObservableProperty] private string _resourceName = string.Empty;

    [ObservableProperty] private int _quantity;
}
