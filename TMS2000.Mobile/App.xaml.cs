using TMS2000.Mobile.Services;
namespace TMS2000.Mobile;

// on est toujours obligé de passer par App.xaml.cs, même si la navigation dans l'app est 100% geré par l'AppNavigator.

public partial class App : Application
{
    private readonly AppNavigator _appNavigator;
    
    public App(AppNavigator appNavigator)
    {
        _appNavigator = appNavigator;
        InitializeComponent();
    }
    
    // MAUI appelle cette méthode au démarrage pour obtenir la fenêtre de l'app : l'AppNavigator la crée et décide de ce qu'elle affiche.
    
    protected override Window CreateWindow(IActivationState? activationState) => _appNavigator.CreateMainWindow();
}