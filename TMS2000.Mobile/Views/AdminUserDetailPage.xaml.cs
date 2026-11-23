using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AdminUserDetailPage : ContentPage
{
    public const string Route = "adminuserdetail";

    private readonly AdminUserDetailViewModel _viewModel;

    public AdminUserDetailPage(AdminUserDetailViewModel viewModel)
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
}
