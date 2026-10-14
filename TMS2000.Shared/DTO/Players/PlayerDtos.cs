using TMS2000.Shared;

namespace TMS2000.Shared.DTO.Players;

public record MeResponse(
    string Email,
    decimal Balance,
    List<PortfolioItemDto> Portfolio,
    List<TransactionDto> RecentTransactions);

public record PortfolioItemDto(
    string ResourceId,
    string ResourceName,
    int Quantity,
    decimal AverageCost,
    decimal CurrentValue,
    decimal UnrealizedProfit);

public record TransactionDto(
    string TransactionId,
    string ResourceId,
    string ResourceName,
    TransactionType Type,
    int Quantity,
    decimal ExecutedPrice,
    DateTime CreatedAt,
    DateTime? ExecutedAt,
    bool Offline,
    TransactionStatus Status,
    string? RejectReason,
    decimal? Profit,
    decimal? Efficiency);

// Utilisés par l'admin (étape 4) : détail des lots

public record HoldingDto(
    string ResourceId,
    string ResourceName,
    List<LotDto> Lots);

public record LotDto(
    int Quantity,
    decimal BuyPrice,
    DateTime BoughtAt);