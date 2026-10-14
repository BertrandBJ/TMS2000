using System.ComponentModel.DataAnnotations;
using TMS2000.Shared;

namespace TMS2000.Shared.DTO.Trades;

public record TradeRequest(
    [Required, StringLength(64)] string TransactionId,   // généré par le mobile (GUID), sert à l'idempotence
    [Required] string ResourceId,
    [Required] TransactionType? Type,                     // nullable + Required : un type absent donne 400, pas "Buy" par défaut
    [Range(1, 1_000_000)] int Quantity);

public record TradeResponse(
    string TransactionId,
    TransactionStatus Status,
    decimal ExecutedPrice,
    decimal? Profit,        // vente uniquement
    decimal? Efficiency,    // vente uniquement
    decimal NewBalance);