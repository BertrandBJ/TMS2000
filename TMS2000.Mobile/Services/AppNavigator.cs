using TMS2000.Mobile.Views;
using TMS2000.Shared;

namespace TMS2000.Mobile.Services;


public class AppNavigator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MarketTickLoop _marketTickLoop;
    private Window? _mainWindow;


    public AppNavigator(IServiceProvider serviceProvider, TokenStore tokenStore, MarketTickLoop marketTickLoop)
    {
        _serviceProvider = serviceProvider;
        _marketTickLoop = marketTickLoop;
        tokenStore.SessionCleared += OnSessionCleared;
    }

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
            Roles.Player => _serviceProvider.GetRequiredService<PlayerShell>(),
            Roles.Admin => _serviceProvider.GetRequiredService<AdminShell>(),
            _ => throw new InvalidOperationException($"Rôle inconnu : {role}")
        };
        SetRootPage(homePage);


        if (role == Roles.Player)
        {
            _marketTickLoop.Start();
        }
    }


    private async void OnMainWindowCreated(object? sender, EventArgs eventArgs)
    {
        await ShowStartPageAsync();
    }


    private async Task ShowStartPageAsync()
    {

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
    private void SetRootPage(Page page)
    {

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_mainWindow is not null)
            {
                _mainWindow.Page = page;
            }
        });
    }


    private void OnSessionCleared(object? sender, EventArgs eventArgs)
    {

        MainThread.BeginInvokeOnMainThread(ShowLogin);

    }
}