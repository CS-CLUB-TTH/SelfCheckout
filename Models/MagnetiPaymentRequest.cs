namespace SelfCheckoutKiosk.Models;

/// <summary>
/// Request model for initiating a payment transaction with Magneti
/// </summary>
public class MagnetiPaymentRequest
{
    /// <summary>
    /// Unique transaction ID from the merchant system
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount in decimal format
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "AED", "USD")
    /// </summary>
    public string Currency { get; set; } = "AED";

    /// <summary>
    /// Description of the transaction
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Customer identifier (optional)
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Timestamp when the transaction was initiated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
