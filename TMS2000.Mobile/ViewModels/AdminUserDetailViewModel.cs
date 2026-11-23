using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TMS2000.Mobile.Helpers;
using TMS2000.Mobile.Services;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Mobile.ViewModels;

public partial class AdminUserDetailViewModel : ObservableObject, IQueryAttributable
{
    public const string UserIdQueryKey = "userId";

    private readonly AdminService _adminService;
    private string? _userId;

    public AdminUserDetailViewModel(AdminService adminService)
    {
        _adminService = adminService;
    }

    [ObservableProperty] private string _email = string.Empty;

    [ObservableProperty] private decimal _balance;

    [ObservableProperty] private List<HoldingRow> _holdingRows = new();

    [ObservableProperty] private bool _hasNoHoldings;

    [ObservableProperty] private List<TransactionRow> _transactionRows = new();

    [ObservableProperty] private bool _hasNoTransactions;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isRefreshing;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(UserIdQueryKey, out object? userIdValue))
        {
            _userId = userIdValue as string;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_userId is null)
        {
            IsRefreshing = false;
            return;
        }

        ErrorMessage = null;
        try
        {
            UserDetailResponse userDetail = await _adminService.GetUserAsync(_userId);

            Email = userDetail.Email;
            Balance = userDetail.Balance;

            HoldingRows = userDetail.Portfolio
                .Where(holding => holding.Lots.Count > 0)
                .OrderBy(holding => holding.ResourceName)
                .Select(holding => new HoldingRow(holding.ResourceId)
                {
                    ResourceName = holding.ResourceName,
                    Quantity = holding.Lots.Sum(lot => lot.Quantity)
                })
                .ToList();
            HasNoHoldings = HoldingRows.Count == 0;

            TransactionRows = userDetail.Transactions
                .Select(transaction =>
                {
                    var transactionRow = new TransactionRow(transaction.TransactionId);
                    transactionRow.UpdateFrom(transaction);
                    return transactionRow;
                })
                .ToList();
            HasNoTransactions = TransactionRows.Count == 0;
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
