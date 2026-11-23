using System.Globalization;

namespace TMS2000.Mobile.Helpers;

public static class DecimalParser
{
    public static bool TryParse(string? text, out decimal value)
    {
        string trimmedText = text?.Trim() ?? string.Empty;

        return decimal.TryParse(trimmedText, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
               || decimal.TryParse(trimmedText, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
