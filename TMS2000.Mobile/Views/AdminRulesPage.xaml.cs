using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AdminRulesPage : ContentPage
{
    private readonly AdminRulesViewModel _viewModel;

    public AdminRulesPage(AdminRulesViewModel viewModel)
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

    private async void OnNewRuleClicked(object? sender, EventArgs eventArgs)
    {
        await Shell.Current.GoToAsync(AdminRuleEditorPage.Route);
    }
}
