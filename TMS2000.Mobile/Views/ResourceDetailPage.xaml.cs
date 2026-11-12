using System.ComponentModel;
using TMS2000.Mobile.ViewModels;

namespace TMS2000.Mobile.Views;

public partial class ResourceDetailPage : ContentPage
{
    public const string Route = "resourcedetail";

    private readonly ResourceDetailViewModel _viewModel;
    private readonly PriceChartDrawable _priceChartDrawable = new();

    public ResourceDetailPage(ResourceDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        PriceChartView.Drawable = _priceChartDrawable;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.StartListening();
        RedrawPriceChart();
    }

    protected override void OnDisappearing()
    {
        _viewModel.StopListening();
        base.OnDisappearing();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(ResourceDetailViewModel.PricePoints))
        {
            RedrawPriceChart();
        }
    }

    private void RedrawPriceChart()
    {
        _priceChartDrawable.PricePoints = _viewModel.PricePoints;
        PriceChartView.Invalidate();
    }
}
