using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AccountPage : ContentPage
{
    private readonly AccountViewModel _viewModel;

    // MAUI crée la page via l'injection de dépendances : il fournit l'AccountViewModel tout seul.

    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshPendingActions();
    }
}
