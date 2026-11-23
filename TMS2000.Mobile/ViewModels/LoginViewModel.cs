using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.API;
using TMS2000.Mobile.Local;
using TMS2000.Mobile.Services;

namespace TMS2000.Mobile.ViewModels;

// État et actions de l'écran de login. Ne connaît pas le XAML : la page se "bind" sur ses propriétés.

public partial class LoginViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly ApiSettings _apiSettings;
    private readonly AppNavigator _appNavigator;
    private readonly CurrentAccount _currentAccount;
    private readonly LocalDatabase _localDatabase;

    public LoginViewModel(
        SessionService sessionService,
        ApiSettings apiSettings,
        AppNavigator appNavigator,
        CurrentAccount currentAccount,
        LocalDatabase localDatabase)
    {
        _sessionService = sessionService;
        _apiSettings = apiSettings;
        _appNavigator = appNavigator;
        _currentAccount = currentAccount;
        _localDatabase = localDatabase;

        _email = currentAccount.Email ?? string.Empty;
        _password = string.Empty;
        _apiBaseUrl = apiSettings.BaseUrl; // pré-rempli avec la dernière URL utilisée

        _ = LoadPendingActionsHintAsync();
    }

    // Chaque champ génère une propriété publique du même nom sans "_" et avec majuscule :
    // _email → Email, _errorMessage → ErrorMessage…

    [ObservableProperty] private string _email = string.Empty;

    [ObservableProperty] private string _password = string.Empty;

    [ObservableProperty] private string _apiBaseUrl;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _pendingActionsHint;

    // Génère la propriété LoginCommand, que le bouton du XAML utilisera.

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "L'email et le mot de passe sont obligatoires.";
            return;
        }

        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
        {
            ErrorMessage = "Adresse de l'API invalide (exemple : http://192.168.1.20:5000).";
            return;
        }

        IsBusy = true;
        try
        {
            _apiSettings.BaseUrl = ApiBaseUrl;
            string role = await _sessionService.LoginAsync(Email, Password);
            Password = string.Empty;
            _appNavigator.ShowHomeForRole(role);
        }
        catch (ApiException apiException)
        {
            ErrorMessage = apiException.Message; // le "detail" écrit par ton GlobalExceptionHandler
        }
        catch (OfflineLoginException offlineLoginException)
        {
            ErrorMessage = offlineLoginException.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Serveur injoignable : vérifie l'adresse de l'API et le réseau.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Le serveur met trop de temps à répondre.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPendingActionsHintAsync()
    {
        string? email = _currentAccount.Email;
        if (email is null)
        {
            return;
        }

        try
        {
            int pendingActionCount = await _localDatabase.CountPendingActionsAsync(email);
            if (pendingActionCount > 0)
            {
                PendingActionsHint = pendingActionCount == 1
                    ? $"1 ordre passé hors ligne avec {email} n'a pas encore été envoyé. Il partira dès que ce compte se connectera en ligne."
                    : $"{pendingActionCount} ordres passés hors ligne avec {email} n'ont pas encore été envoyés. Ils partiront dès que ce compte se connectera en ligne.";
            }
        }
        catch (Exception)
        {
        }
    }
}
