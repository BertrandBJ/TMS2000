using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public partial class AdminUsersViewModel : ObservableObject
{
    private readonly AdminService _adminService;

    public AdminUsersViewModel(AdminService adminService)
    {
        _adminService = adminService;
    }

    [ObservableProperty] private List<UserSummaryDto> _users = new();

    [ObservableProperty] private bool _hasNoUsers;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefreshing;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        try
        {
            UsersResponse usersResponse = await _adminService.GetUsersAsync();
            Users = usersResponse.Users.OrderBy(user => user.Email).ToList();
            HasNoUsers = Users.Count == 0;
        }
        catch (Exception exception) when (ApiErrorMessages.IsKnown(exception))
        {
            ErrorMessage = ApiErrorMessages.Describe(exception);
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
