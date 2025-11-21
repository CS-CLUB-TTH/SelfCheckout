namespace SelfCheckoutKiosk.Models;

/// <summary>
/// Response model from Magneti payment gateway
/// </summary>
public class MagnetiPaymentResponse
{
    /// <summary>
    /// Indicates if the payment was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Payment status (Approved, Declined, Pending, Error, etc.)
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Transaction ID from merchant system
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Authorization code from the payment gateway
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Reference number from Magneti
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Amount that was processed
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "AED";

    /// <summary>
    /// Error message if the payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if the payment failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Card type used (Visa, Mastercard, etc.) - masked/partial info only
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// Last 4 digits of the card (for receipt purposes)
    /// </summary>
    public string? CardLast4 { get; set; }
}

/// <summary>
/// Payment status enumeration
/// </summary>
public enum PaymentStatus
{
    Pending,
    Approved,
    Declined,
    Error,
    Timeout,
    Cancelled
}
