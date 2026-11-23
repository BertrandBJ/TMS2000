using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public partial class AdminResourceEditorViewModel : ObservableObject, IQueryAttributable
{
    public const string ResourceQueryKey = "resource";

    private readonly AdminService _adminService;
    private string? _resourceId;

    public AdminResourceEditorViewModel(AdminService adminService)
    {
        _adminService = adminService;
    }

    public event EventHandler? PriceBandSaved;

    [ObservableProperty] private string _resourceName = string.Empty;

    [ObservableProperty] private string _currentPriceDescription = string.Empty;

    [ObservableProperty] private string _basePriceText = string.Empty;

    [ObservableProperty] private string _minPriceText = string.Empty;

    [ObservableProperty] private string _maxPriceText = string.Empty;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isBusy;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(ResourceQueryKey, out object? resourceValue) || resourceValue is not AdminResourceDto resource)
        {
            return;
        }

        _resourceId = resource.Id;
        ResourceName = resource.Name;
        CurrentPriceDescription = $"Prix actuel : {AmountFormatter.FormatMoney(resource.CurrentPrice)}";
        BasePriceText = resource.BasePrice > 0 ? resource.BasePrice.ToString("0.##") : string.Empty;
        MinPriceText = resource.MinPrice > 0 ? resource.MinPrice.ToString("0.##") : string.Empty;
        MaxPriceText = resource.MaxPrice > 0 ? resource.MaxPrice.ToString("0.##") : string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || _resourceId is null)
        {
            return;
        }

        ErrorMessage = null;

        if (!DecimalParser.TryParse(BasePriceText, out decimal basePrice) || basePrice <= 0)
        {
            ErrorMessage = "Le prix de référence doit être un nombre positif.";
            return;
        }

        if (!TryParseOptionalPrice(MinPriceText, out decimal? minPrice) || !TryParseOptionalPrice(MaxPriceText, out decimal? maxPrice))
        {
            ErrorMessage = "Le minimum et le maximum doivent être des nombres positifs, ou rester vides.";
            return;
        }

        IsBusy = true;
        try
        {
            await _adminService.UpdatePriceBandAsync(_resourceId, new UpdatePriceBandRequest(basePrice, minPrice, maxPrice));
            PriceBandSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool TryParseOptionalPrice(string text, out decimal? price)
    {
        price = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!DecimalParser.TryParse(text, out decimal parsedPrice) || parsedPrice <= 0)
        {
            return false;
        }

        price = parsedPrice;
        return true;
    }
}
