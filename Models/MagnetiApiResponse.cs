namespace SelfCheckoutKiosk.Models;

/// <summary>
/// Internal model for Magneti API response
/// Maps to the JSON structure returned by Magneti payment gateway API
/// </summary>
public class MagnetiApiResponse
{
    public string? Status { get; set; }
    public string? TransactionId { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? CardType { get; set; }
    public string? CardLast4 { get; set; }
}
