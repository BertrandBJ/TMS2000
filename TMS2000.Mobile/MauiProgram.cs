using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TMS2000.Mobile.API;
using TMS2000.Mobile.ViewModels;
using TMS2000.Mobile.Services;
using TMS2000.Mobile.Views;
namespace TMS2000.Mobile;

public static class MauiProgram
{
    private static readonly TimeSpan ApiRequestTimeout = TimeSpan.FromSeconds(10);

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        RegisterServices(builder.Services);
        RegisterApiClients(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);
        RegisterRoutes();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AppNavigator>();
        services.AddTransient<SessionService>();
        services.AddSingleton<ApiSettings>();
        services.AddSingleton<TokenStore>();
        services.AddSingleton<SnapshotStore>();
        services.AddTransient<SnapshotService>();
        services.AddSingleton<MarketTickLoop>();
        services.AddTransient<TradeService>();
    }

    // Les deux clients HTTP : même configuration de base, seul ApiClient passe par l'AuthHandler.
    private static void RegisterApiClients(IServiceCollection services)
    {
        services.AddTransient<AuthHandler>(); // transient => la factory exige un handle neuf chaque fois qu'elle build

        services.AddHttpClient<AuthApiClient>(ConfigureApiHttpClientTimeout);

        // => le nom de ce type de synthax c'est "method groupe". ça dit : 
        // factory ! chaquefois que tu dois faire cette action, appel également la methode suivante dessus.

        services.AddHttpClient<ApiClient>(ConfigureApiHttpClientTimeout)
            .AddHttpMessageHandler<AuthHandler>();
    }

    private static void ConfigureApiHttpClientTimeout(HttpClient httpClient)
    {
        httpClient.Timeout = ApiRequestTimeout;
    }

    // ViewModels : une instance neuve à chaque ouverture d'écran.
    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AccountViewModel>();
        services.AddTransient<MarketViewModel>();
        services.AddTransient<ResourceDetailViewModel>();
        services.AddTransient<PortfolioViewModel>();
        services.AddTransient<TransactionsViewModel>();
    }

    // Pages : enregistrées pour que MAUI leur injecte leur ViewModel.
    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<LoginPage>();
        services.AddTransient<PlayerShell>();
        services.AddTransient<AccountPage>();
        services.AddTransient<MarketPage>();
        services.AddTransient<ResourceDetailPage>();
        services.AddTransient<PortfolioPage>();
        services.AddTransient<TransactionsPage>();
        services.AddTransient<AdminShell>();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(ResourceDetailPage.Route, typeof(ResourceDetailPage));
    }
}