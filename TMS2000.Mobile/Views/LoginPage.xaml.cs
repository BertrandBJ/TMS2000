using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class LoginPage : ContentPage
{
    // MAUI crée la page via l'injection de dépendances : il fournit le LoginViewModel tout seul.
    
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();       // construit les contrôles décrits dans LoginPage.xaml
        BindingContext = viewModel;  // les {Binding …} du XAML portent sur cet objet
    }
}