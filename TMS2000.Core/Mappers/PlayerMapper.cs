using TMS2000.Core.Models;
using TMS2000.Shared.DTO.Players;

namespace TMS2000.Core.Mappers;

public static class PlayerMapper
{
    public static TransactionDto ToTransactionDto(Transaction transaction) => new(
        
        TransactionId: transaction.TransactionId,
        ResourceId: transaction.ResourceId,
        ResourceName: transaction.ResourceName,
        Type: transaction.Type,
        Quantity: transaction.Quantity,
        ExecutedPrice: transaction.ExecutedPrice,
        CreatedAt: transaction.CreatedAt,
        ExecutedAt: transaction.ExecutedAt,
        Offline: transaction.Offline,
        Status: transaction.Status,
        RejectReason: transaction.RejectReason,
        Profit: transaction.Profit,
        Efficiency: transaction.Efficiency);

    public static HoldingDto ToHoldingDto(Holding holding) => new(
        
        ResourceId: holding.ResourceId,
        ResourceName: holding.ResourceName,
        Lots: holding.Lots.Select(lot => new LotDto(lot.Quantity, lot.BuyPrice, lot.BoughtAt)).ToList());
}