namespace TMS2000.Core.Models;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public decimal ExecutedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public bool Offline { get; set; }
    public TransactionStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public decimal? Profit { get; set; }
    public decimal? Efficiency { get; set; }
}