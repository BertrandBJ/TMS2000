namespace TMS2000.Core;

public static class GameSettings
{
    public const int MaxTransactionsPerUser = 200;     // historique gardé par joueur
    public const int MaxPricePoints = 5000;            // historique de prix gardé par ressource
    public const int RecentTransactionsCount = 20;     // transactions renvoyées par GET /api/me
    public const int MaxRulesPerResource = 3;          // règles actives ou à venir par ressource
    public const decimal MaxTaxPercent = 50m;          // taxe max d'une règle TransactionTax
    public const decimal MinPrice = 1m;                // un prix ne descend jamais sous 1
    public const int TradeMaxAttempts = 3;             // tentatives en cas de modification concurrente
    public const int MaxSessionsPerUser = 5;           // pour le refresh

    public const int MaxOfflineDays = 30;                    // au-delà, une action offline est rejetée
    public const decimal OfflineBonusPercentPerHour = 1m;    // +1 % du profit par heure hors ligne
    public const decimal OfflineBonusMaxPercent = 20m;       // plafonné à +20 %
    public const int ClockSkewToleranceMinutes = 5;          // tolérance d'horloge du téléphone
}