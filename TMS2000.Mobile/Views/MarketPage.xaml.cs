using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class MarketPage : ContentPage
{
    private readonly MarketViewModel _viewModel;


    public MarketPage(MarketViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.StartListening();
    }


    protected override void OnDisappearing()
    {
        _viewModel.StopListening();
        base.OnDisappearing();
    }

    private async void OnResourceSelected(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (eventArgs.CurrentSelection.FirstOrDefault() is not MarketRow selectedMarketRow)
        {
            return;
        }

        if (sender is CollectionView resourceList)
        {
            resourceList.SelectedItem = null;
        }

        var navigationParameters = new Dictionary<string, object>
        {
            [ResourceDetailViewModel.ResourceIdQueryKey] = selectedMarketRow.ResourceId
        };
        await Shell.Current.GoToAsync(ResourceDetailPage.Route, navigationParameters);
    }
}
