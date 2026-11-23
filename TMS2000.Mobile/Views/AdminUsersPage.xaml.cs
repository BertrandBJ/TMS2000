using TMS2000.Mobile.ViewModels;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.Views;

public partial class AdminUsersPage : ContentPage
{
    private readonly AdminUsersViewModel _viewModel;

    public AdminUsersPage(AdminUsersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshCommand.Execute(null);
    }

    private async void OnUserSelected(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (eventArgs.CurrentSelection.FirstOrDefault() is not UserSummaryDto selectedUser)
        {
            return;
        }

        if (sender is CollectionView userList)
        {
            userList.SelectedItem = null;
        }

        var navigationParameters = new Dictionary<string, object>
        {
            [AdminUserDetailViewModel.UserIdQueryKey] = selectedUser.Id
        };
        await Shell.Current.GoToAsync(AdminUserDetailPage.Route, navigationParameters);
    }
}
