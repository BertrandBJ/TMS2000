using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class AdminResourcesPage : ContentPage
{
    private readonly AdminResourcesViewModel _viewModel;

    public AdminResourcesPage(AdminResourcesViewModel viewModel)
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

    private async void OnResourceSelected(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (eventArgs.CurrentSelection.FirstOrDefault() is not AdminResourceRow selectedResourceRow)
        {
            return;
        }

        if (sender is CollectionView resourceList)
        {
            resourceList.SelectedItem = null;
        }

        var navigationParameters = new Dictionary<string, object>
        {
            [AdminResourceEditorViewModel.ResourceQueryKey] = selectedResourceRow.Resource
        };
        await Shell.Current.GoToAsync(AdminResourceEditorPage.Route, navigationParameters);
    }
}
