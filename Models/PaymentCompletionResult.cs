namespace SelfCheckoutKiosk.Models;

/// <summary>
/// Result of completing a self-checkout payment
/// </summary>
public class PaymentCompletionResult
{
    public int BillHdrKey { get; set; }
    public decimal? BillAmt { get; set; }
    public decimal? BillDiscAmt { get; set; }
    public decimal? TotalVat { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    public bool IsSuccess => Status == "SUCCESS";
}
