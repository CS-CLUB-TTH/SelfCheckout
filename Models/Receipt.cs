namespace SelfCheckoutKiosk.Models;

/// <summary>
/// Receipt model containing all transaction and cart information for printing
/// </summary>
public class Receipt
{
    /// <summary>
    /// Transaction identifier
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction date and time
    /// </summary>
    public DateTime TransactionDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Store or kiosk name
    /// </summary>
    public string StoreName { get; set; } = "Self Checkout Kiosk";

    /// <summary>
    /// Customer identifier (optional)
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// List of items purchased
    /// </summary>
    public List<ReceiptItem> Items { get; set; } = new();

    /// <summary>
    /// Subtotal amount (before tax)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax/VAT amount
    /// </summary>
    public decimal Tax { get; set; }

    /// <summary>
    /// Total amount (subtotal + tax)
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Payment authorization code
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Payment reference number
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Card type used for payment
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// Last 4 digits of the card
    /// </summary>
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "AED";
}

/// <summary>
/// Individual item on a receipt
/// </summary>
public class ReceiptItem
{
    /// <summary>
    /// Product description/name
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity purchased
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Price per unit
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Line total (quantity * price)
    /// </summary>
    public decimal Amount { get; set; }
}
