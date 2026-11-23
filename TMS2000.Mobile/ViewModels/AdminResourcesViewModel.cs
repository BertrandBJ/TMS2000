using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public partial class AdminResourcesViewModel : ObservableObject
{
    private readonly AdminService _adminService;

    public AdminResourcesViewModel(AdminService adminService)
    {
        _adminService = adminService;
    }

    [ObservableProperty] private List<AdminResourceRow> _resourceRows = new();

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefreshing;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        try
        {
            AdminResourcesResponse resourcesResponse = await _adminService.GetResourcesAsync();
            ResourceRows = resourcesResponse.Resources.Select(ToAdminResourceRow).ToList();
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private static AdminResourceRow ToAdminResourceRow(AdminResourceDto resource)
    {
        bool hasPriceBand = resource.BasePrice > 0 && resource.MinPrice > 0 && resource.MaxPrice > 0;
        string priceBandDescription = hasPriceBand
            ? $"Référence {AmountFormatter.FormatMoney(resource.BasePrice)} · "
              + $"{AmountFormatter.FormatMoney(resource.MinPrice)} – {AmountFormatter.FormatMoney(resource.MaxPrice)}"
            : "Aucune fourchette définie";

        return new AdminResourceRow(
            Resource: resource,
            CurrentPriceDescription: AmountFormatter.FormatMoney(resource.CurrentPrice),
            PriceModeDescription: PriceModeDescriber.Describe(resource.Mode),
            PriceBandDescription: priceBandDescription);
    }
}
