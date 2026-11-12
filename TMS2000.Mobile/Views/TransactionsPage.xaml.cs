using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;

    public TransactionsPage(TransactionsViewModel viewModel)
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
