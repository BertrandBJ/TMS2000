namespace TMS2000.Core.Services;

public class MarketClock
{
    public DateTime NextTickAt { get; set; } = DateTime.UtcNow;
}