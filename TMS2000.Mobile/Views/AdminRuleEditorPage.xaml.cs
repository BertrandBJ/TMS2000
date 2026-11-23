using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AdminRuleEditorPage : ContentPage
{
    public const string Route = "adminruleeditor";

    private readonly AdminRuleEditorViewModel _viewModel;

    public AdminRuleEditorPage(AdminRuleEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.RuleCreated += OnRuleCreated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadResourcesCommand.Execute(null);
    }

    private async void OnRuleCreated(object? sender, EventArgs eventArgs)
    {
        await Shell.Current.GoToAsync("..");
    }
}
