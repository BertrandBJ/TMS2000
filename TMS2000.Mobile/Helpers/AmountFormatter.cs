namespace TMS2000.Mobile.Helpers;

public static class AmountFormatter
{
    public static string FormatSigned(decimal amount) => $"{amount:+#,##0;-#,##0;0} $";
}
