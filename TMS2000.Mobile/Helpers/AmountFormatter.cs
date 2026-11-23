namespace TMS2000.Mobile.Helpers;

public static class AmountFormatter
{
    private const decimal DecimalsThreshold = 100m;

    public static string FormatMoney(decimal amount) =>
        Math.Abs(amount) >= DecimalsThreshold ? $"{amount:N0} $" : $"{amount:N2} $";

    public static string FormatSigned(decimal amount) =>
        Math.Abs(amount) >= DecimalsThreshold
            ? $"{amount:+#,##0;-#,##0;0} $"
            : $"{amount:+#,##0.00;-#,##0.00;0.00} $";
}
