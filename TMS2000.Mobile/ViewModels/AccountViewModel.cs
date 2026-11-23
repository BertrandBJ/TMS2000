using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Services;

namespace TMS2000.Mobile.ViewModels;

// Onglet "Compte", commun au joueur et à l'admin : pour l'instant, seulement la déconnexion.

public partial class AccountViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly PendingActionQueue _pendingActionQueue;

    public AccountViewModel(SessionService sessionService, PendingActionQueue pendingActionQueue)
    {
        _sessionService = sessionService;
        _pendingActionQueue = pendingActionQueue;
    }

    [ObservableProperty] private string? _pendingActionsDescription;

    [ObservableProperty] private bool _hasPendingActions;

    public void RefreshPendingActions()
    {
        int pendingActionCount = _pendingActionQueue.Count;
        HasPendingActions = pendingActionCount > 0;
        PendingActionsDescription = DescribePendingActions(pendingActionCount);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        int pendingActionCount = _pendingActionQueue.Count;
        if (pendingActionCount > 0 && Shell.Current is Shell currentShell)
        {
            bool logoutConfirmed = await currentShell.DisplayAlertAsync(
                "Ordres en attente",
                $"{DescribePendingActions(pendingActionCount)} Ils restent enregistrés sur ce téléphone et seront envoyés "
                + "à la prochaine connexion en ligne avec ce compte. Se déconnecter quand même ?",
                "Se déconnecter",
                "Annuler");

            if (!logoutConfirmed)
            {
                return;
            }
        }

        await _sessionService.LogoutAsync();
    }

    private static string? DescribePendingActions(int pendingActionCount) => pendingActionCount switch
    {
        0 => null,
        1 => "1 ordre passé hors ligne n'a pas encore été envoyé au serveur.",
        _ => $"{pendingActionCount} ordres passés hors ligne n'ont pas encore été envoyés au serveur."
    };
}
