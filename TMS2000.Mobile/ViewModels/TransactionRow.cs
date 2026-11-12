using CommunityToolkit.Mvvm.ComponentModel;
using TMS2000.Mobile.Helpers;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Players;

namespace TMS2000.Mobile.ViewModels;

public partial class TransactionRow : ObservableObject
{
    public TransactionRow(string transactionId)
    {
        TransactionId = transactionId;
    }

    public string TransactionId { get; }

    [ObservableProperty] private string _typeDescription = string.Empty;

    [ObservableProperty] private string _resourceName = string.Empty;

    [ObservableProperty] private string _tradeDescription = string.Empty;

    [ObservableProperty] private string _dateDescription = string.Empty;

    [ObservableProperty] private string _resultDescription = string.Empty;

    [ObservableProperty] private Color _accentColor = Colors.Gray;

    public void UpdateFrom(TransactionDto transaction)
    {
        bool isPurchase = transaction.Type == TransactionType.Buy;
        DateTime transactionDate = (transaction.ExecutedAt ?? transaction.CreatedAt).ToLocalTime();

        TypeDescription = isPurchase ? "🛒 Achat" : "💰 Vente";
        ResourceName = transaction.ResourceName;
        TradeDescription = $"{transaction.Quantity} × {transaction.ExecutedPrice:N0} $";
        DateDescription = transaction.Offline
            ? $"{transactionDate:dd/MM HH:mm} · hors ligne"
            : $"{transactionDate:dd/MM HH:mm}";
        ResultDescription = DescribeResult(transaction);
        AccentColor = ChooseAccentColor(transaction);
    }

    private static string DescribeResult(TransactionDto transaction)
    {
        if (transaction.Status == TransactionStatus.Rejected)
        {
            return $"Refusée : {transaction.RejectReason}";
        }

        if (transaction.Status == TransactionStatus.Pending)
        {
            return "En attente de synchronisation";
        }

        if (transaction.Type == TransactionType.Buy)
        {
            return $"Total {transaction.Quantity * transaction.ExecutedPrice:N0} $";
        }

        decimal profit = transaction.Profit ?? 0;
        string profitDescription = profit < 0
            ? $"Perte {AmountFormatter.FormatSigned(profit)}"
            : $"Gain {AmountFormatter.FormatSigned(profit)}";

        return transaction.Efficiency is decimal efficiency
            ? $"{profitDescription} · efficacité {efficiency:P0}"
            : profitDescription;
    }

    private static Color ChooseAccentColor(TransactionDto transaction)
    {
        if (transaction.Status == TransactionStatus.Rejected)
        {
            return ResultColors.Rejected;
        }

        if (transaction.Status == TransactionStatus.Pending || transaction.Type == TransactionType.Buy)
        {
            return ResultColors.Neutral;
        }

        return ResultColors.ForSale(transaction.Profit ?? 0, transaction.Efficiency);
    }
}

