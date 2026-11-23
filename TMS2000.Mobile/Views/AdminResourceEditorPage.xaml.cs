using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AdminResourceEditorPage : ContentPage
{
    public const string Route = "adminresourceeditor";

    private readonly AdminResourceEditorViewModel _viewModel;

    public AdminResourceEditorPage(AdminResourceEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.PriceBandSaved += OnPriceBandSaved;
    }

    private async void OnPriceBandSaved(object? sender, EventArgs eventArgs)
    {
        await Shell.Current.GoToAsync("..");
    }
}
