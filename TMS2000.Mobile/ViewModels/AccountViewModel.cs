using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Services;

namespace TMS2000.Mobile.ViewModels;

// Onglet "Compte", commun au joueur et à l'admin : pour l'instant, seulement la déconnexion.

public partial class AccountViewModel : ObservableObject
{
    private readonly SessionService _sessionService;

    public AccountViewModel(SessionService sessionService)
    {
        _sessionService = sessionService;
    }
    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _sessionService.LogoutAsync();
    }
}