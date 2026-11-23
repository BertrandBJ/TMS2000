using SQLite;
using TMS2000.Shared;
using TMS2000.Shared.DTO.Players;
using TMS2000.Shared.DTO.Sync;

namespace TMS2000.Mobile.Local;

public class PendingAction
{
    [PrimaryKey]
    public string TransactionId { get; set; } = string.Empty;

    [Indexed]
    public string Email { get; set; } = string.Empty;

    public string ResourceId { get; set; } = string.Empty;

    public string ResourceName { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal EstimatedPrice { get; set; }

    [Ignore]
    public DateTime CreatedAtUtc => DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc);

    public OfflineAction ToOfflineAction() =>
        new(TransactionId, ResourceId, Type, Quantity, CreatedAtUtc);

    public TransactionDto ToTransactionDto() =>
        new(TransactionId,
            ResourceId,
            ResourceName,
            Type,
            Quantity,
            EstimatedPrice,
            CreatedAtUtc,
            null,
            true,
            TransactionStatus.Pending,
            null,
            null,
            null);
}
