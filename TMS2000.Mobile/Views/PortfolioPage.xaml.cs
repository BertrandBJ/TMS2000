using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class PortfolioPage : ContentPage
{
    private readonly PortfolioViewModel _viewModel;

    public PortfolioPage(PortfolioViewModel viewModel)
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
}
