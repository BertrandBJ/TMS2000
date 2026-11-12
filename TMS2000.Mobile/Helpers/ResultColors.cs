namespace TMS2000.Mobile.Helpers;

public static class ResultColors
{
    public static readonly Color Neutral = Colors.Gray;
    public static readonly Color Gain = Colors.SeaGreen;
    public static readonly Color Average = Colors.Goldenrod;
    public static readonly Color Weak = Colors.DarkOrange;
    public static readonly Color Loss = Colors.OrangeRed;
    public static readonly Color Rejected = Colors.Red;

    private const decimal GoodEfficiencyThreshold = 0.66m;
    private const decimal AverageEfficiencyThreshold = 0.33m;

    public static Color ForProfit(decimal profit) => profit < 0 ? Loss : Gain;

    public static Color ForEfficiency(decimal efficiency)
    {
        if (efficiency >= GoodEfficiencyThreshold)
        {
            return Gain;
        }

        if (efficiency >= AverageEfficiencyThreshold)
        {
            return Average;
        }

        return efficiency >= 0 ? Weak : Loss;
    }

    public static Color ForSale(decimal profit, decimal? efficiency)
    {
        if (profit < 0)
        {
            return Loss;
        }

        return efficiency is decimal knownEfficiency ? ForEfficiency(knownEfficiency) : Neutral;
    }
}
