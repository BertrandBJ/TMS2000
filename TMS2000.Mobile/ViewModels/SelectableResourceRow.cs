using CommunityToolkit.Mvvm.ComponentModel;

namespace TMS2000.Mobile.ViewModels;

public partial class SelectableResourceRow : ObservableObject
{
    public SelectableResourceRow(string resourceId, string resourceName)
    {
        ResourceId = resourceId;
        ResourceName = resourceName;
    }

    public string ResourceId { get; }

    public string ResourceName { get; }

    [ObservableProperty] private bool _isSelected;
}
