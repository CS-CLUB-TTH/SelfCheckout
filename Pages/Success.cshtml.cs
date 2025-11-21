using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SelfCheckoutKiosk.Pages;

public class SuccessModel : PageModel
{
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CardType { get; set; }
    public string? CardLast4 { get; set; }

    public void OnGet()
    {
        // Retrieve payment details from TempData
        TransactionId = TempData["PaymentTransactionId"] as string;
        Amount = TempData["PaymentAmount"] as decimal? ?? 0;
        AuthorizationCode = TempData["PaymentAuthCode"] as string;
        ReferenceNumber = TempData["PaymentReferenceNumber"] as string;
        CardType = TempData["PaymentCardType"] as string;
        CardLast4 = TempData["PaymentCardLast4"] as string;
    }
}
