using System.Net.Http.Json;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Players;
using TMS2000.Shared.DTO.Sync;
using TMS2000.Shared.DTO.Trades;
using TMS2000.Shared.DTO.Admin;

namespace TMS2000.Tests.Infrastructure;

// Appels à l'API utilisés par plusieurs classes de test.
// Importée avec "using static", cette classe s'utilise comme si ses méthodes étaient dans la classe de test.
public static class ApiCalls
{
    public static Task<HttpResponseMessage> PostTradeAsync(HttpClient client, string transactionId, string resourceId, TransactionType type, int quantity) =>
        client.PostAsJsonAsync("/api/trades", new TradeRequest(transactionId, resourceId, type, quantity), TestApiFactory.JsonOptions);

    public static Task<HttpResponseMessage> PostSyncPushAsync(HttpClient client, params OfflineAction[] actions) =>
        client.PostAsJsonAsync("/api/sync", new SyncPushRequest(actions.ToList()), TestApiFactory.JsonOptions);

    public static async Task<MeResponse> GetProfileAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<MeResponse>("/api/me", TestApiFactory.JsonOptions))!;

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(TestApiFactory.JsonOptions))!;


    // La condition par défaut ("prix > 1") est toujours vraie : la règle s'applique dès qu'elle est active.
    // conditionOperator et conditionValue permettent de tester une règle dont la condition n'est PAS remplie.

    public static Task<HttpResponseMessage> CreateRuleAsync(
        HttpClient client,
        string resourceId,
        EffectType effectType,
        decimal? effectValue = null,
        int durationHours = 1,
        DateTime? startsAt = null,
        ConditionOperator conditionOperator = ConditionOperator.GreaterThan,
        decimal conditionValue = 1m)
    {
        var request = new CreateRuleRequest(

            ResourceIds: new List<string> { resourceId },
            Condition: new RuleConditionDto("price", conditionOperator, conditionValue),
            Effect: new RuleEffectDto(effectType, effectValue),
            StartsAt: startsAt,
            DurationHours: durationHours);

        return client.PostAsJsonAsync("/api/admin/rules", request, TestApiFactory.JsonOptions);
    }
}