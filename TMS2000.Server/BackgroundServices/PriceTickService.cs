using TMS2000.Core.Services;

namespace TMS2000.Server.BackgroundServices;

public class PriceTickService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MarketClock _marketClock;
    private readonly ILogger<PriceTickService> _logger;
    private readonly TimeSpan _interval;

    public PriceTickService(
        IServiceScopeFactory scopeFactory,
        MarketClock marketClock,
        ILogger<PriceTickService> logger,
        TimeSpan interval)
    {
        _scopeFactory = scopeFactory;
        _marketClock = marketClock;
        _logger = logger;
        _interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _marketClock.NextTickAt = DateTime.UtcNow.Add(_interval);
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTime.UtcNow;
            _marketClock.NextTickAt = now.Add(_interval);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var priceEngine = scope.ServiceProvider.GetRequiredService<PriceEngineService>();
                await priceEngine.TickAsync(now);

                _logger.LogInformation("Tick de prix appliqué à {Time:HH:mm:ss} UTC", now);
            }
            catch (Exception exception)
            {
                // Un tick raté ne doit pas tuer le service : on log et on attend le suivant
                _logger.LogError(exception, "Échec du tick de prix");
            }
        }
    }
}