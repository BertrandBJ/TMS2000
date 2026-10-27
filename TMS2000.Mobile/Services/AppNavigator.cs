using TMS2000.Mobile.Views;
using TMS2000.Shared;

namespace TMS2000.Mobile.Services;

// Seul responsable de ce qu'affiche l'app : il crée la fenêtre, puis remplace sa page racine
// (attente au démarrage → login ou accueil selon le rôle).
// Singleton : il ne garde aucun client HTTP, seulement de quoi créer les pages.
public class AppNavigator
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _mainWindow;

    public AppNavigator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Appelée une seule fois, par App, quand MAUI demande la fenêtre de l'app.
    public Window CreateMainWindow()
    {
        var startupPage = new ContentPage
        {
            Content = new ActivityIndicator
            {
                IsRunning = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        _mainWindow = new Window(startupPage);
        _mainWindow.Created += OnMainWindowCreated;
        return _mainWindow;
    }

    public void ShowLogin()
    {
        SetRootPage(_serviceProvider.GetRequiredService<LoginPage>());
    }

    public void ShowHomeForRole(string role)
    {
        Page homePage = role switch
        {
            Roles.Player => CreateTemporaryHomePage("Joueur"),
            Roles.Admin => CreateTemporaryHomePage("Administrateur"),
            _ => throw new InvalidOperationException($"Rôle inconnu : {role}")
        };
        SetRootPage(homePage);
    }

    // Déclenché par MAUI quand la fenêtre native Android est prête.
    private async void OnMainWindowCreated(object? sender, EventArgs eventArgs)
    {
        await ShowStartPageAsync();
    }

    // Reprend la session sauvegardée s'il y en a une, sinon affiche le login.
    private async Task ShowStartPageAsync()
    {
        // Demandé ici et pas injecté dans le constructeur :
        // SessionService est transient (il utilise un client HTTP), AppNavigator est un singleton.
        var sessionService = _serviceProvider.GetRequiredService<SessionService>();

        string? savedRole = await sessionService.GetSavedSessionRoleAsync();
        if (savedRole is null)
        {
            ShowLogin();
        }
        else
        {
            ShowHomeForRole(savedRole);
        }
    }

    // TEMPORAIRE : sera remplacé par PlayerShell / AdminShell juste après le premier test du login.
    private static Page CreateTemporaryHomePage(string roleLabel) =>
        new ContentPage
        {
            Content = new Label
            {
                Text = $"Connecté en tant que {roleLabel}",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

    private void SetRootPage(Page page)
    {
        // L'interface ne peut être modifiée que depuis le thread principal (UI thread).
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_mainWindow is not null)
            {
                _mainWindow.Page = page;
            }
        });
    }
}