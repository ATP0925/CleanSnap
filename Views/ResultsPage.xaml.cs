using CleanSnap.ViewModels;

namespace CleanSnap.Views;

public partial class ResultsPage : ContentPage
{
    private readonly ResultsViewModel _viewModel;
    private readonly HomeViewModel _homeViewModel;

    public ResultsPage(ResultsViewModel viewModel, HomeViewModel homeViewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _homeViewModel = homeViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load latest scan result from HomeViewModel if available
        if (_homeViewModel.LastScanResult != null || _homeViewModel.LastJunkAnalysis != null)
        {
            _viewModel.LoadResults(_homeViewModel.LastScanResult, _homeViewModel.LastJunkAnalysis);
        }
    }
}

