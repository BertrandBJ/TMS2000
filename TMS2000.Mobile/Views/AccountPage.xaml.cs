using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AccountPage : ContentPage
{
    // MAUI crée la page via l'injection de dépendances : il fournit l'AccountViewModel tout seul.

    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}