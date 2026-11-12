using TMS2000.Shared.DTO.Market;

namespace TMS2000.Mobile.Views;

public class PriceChartDrawable : IDrawable
{
    private const float ChartPadding = 8f;
    private const float PriceLabelHeight = 16f;
    private const float PriceLabelFontSize = 12f;

    public List<PricePointDto> PricePoints { get; set; } = new();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        List<PricePointDto> orderedPricePoints = PricePoints.OrderBy(pricePoint => pricePoint.At).ToList();
        if (orderedPricePoints.Count < 2)
        {
            return;
        }

        decimal lowestPrice = orderedPricePoints.Min(pricePoint => pricePoint.Price);
        decimal highestPrice = orderedPricePoints.Max(pricePoint => pricePoint.Price);
        decimal priceRange = highestPrice - lowestPrice;
        if (priceRange == 0)
        {
            priceRange = 1;
        }

        float chartLeft = dirtyRect.Left + ChartPadding;
        float chartRight = dirtyRect.Right - ChartPadding;
        float chartTop = dirtyRect.Top + ChartPadding;
        float chartBottom = dirtyRect.Bottom - ChartPadding;
        float chartWidth = chartRight - chartLeft;
        float chartHeight = chartBottom - chartTop;
        float horizontalStep = chartWidth / (orderedPricePoints.Count - 1);

        var priceLine = new PathF();
        for (int pointIndex = 0; pointIndex < orderedPricePoints.Count; pointIndex++)
        {
            float pointX = chartLeft + pointIndex * horizontalStep;
            float heightRatio = (float)((orderedPricePoints[pointIndex].Price - lowestPrice) / priceRange);
            float pointY = chartBottom - heightRatio * chartHeight;

            if (pointIndex == 0)
            {
                priceLine.MoveTo(pointX, pointY);
            }
            else
            {
                priceLine.LineTo(pointX, pointY);
            }
        }

        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 1;
        canvas.DrawLine(chartLeft, chartTop, chartRight, chartTop);
        canvas.DrawLine(chartLeft, chartBottom, chartRight, chartBottom);

        canvas.StrokeColor = Colors.DodgerBlue;
        canvas.StrokeSize = 2;
        canvas.DrawPath(priceLine);

        canvas.FontColor = Colors.Gray;
        canvas.FontSize = PriceLabelFontSize;
        canvas.DrawString($"{highestPrice:N0} $",
            chartLeft, chartTop + 2, chartWidth, PriceLabelHeight,
            Microsoft.Maui.Graphics.HorizontalAlignment.Right, Microsoft.Maui.Graphics.VerticalAlignment.Top);
        canvas.DrawString($"{lowestPrice:N0} $",
            chartLeft, chartBottom - PriceLabelHeight - 2, chartWidth, PriceLabelHeight,
            Microsoft.Maui.Graphics.HorizontalAlignment.Right, Microsoft.Maui.Graphics.VerticalAlignment.Bottom);
    }
}
