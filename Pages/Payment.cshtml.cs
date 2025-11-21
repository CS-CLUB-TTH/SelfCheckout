using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SelfCheckoutKiosk.Models;
using SelfCheckoutKiosk.Services;

namespace SelfCheckoutKiosk.Pages;

public class PaymentModel : PageModel
{
    private readonly IMagnetiPaymentService _paymentService;
    private readonly ILogger<PaymentModel> _logger;

    public PaymentModel(IMagnetiPaymentService paymentService, ILogger<PaymentModel> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public decimal Amount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public int? CustomerId { get; set; }
    public int ItemCount { get; set; }
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        // Retrieve cart information from TempData
        if (TempData["CartTotal"] is decimal total)
        {
            Amount = total;
            Subtotal = TempData["CartSubtotal"] as decimal? ?? 0;
            Tax = TempData["CartTax"] as decimal? ?? 0;
            CustomerId = TempData["CustomerId"] as int?;
            ItemCount = TempData["CartItemCount"] as int? ?? 0;

            // Keep TempData for potential retry
            TempData.Keep();

            return Page();
        }

        // No cart data - redirect to home
        _logger.LogWarning("Payment page accessed without cart data");
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Process payment through Magneti gateway
    /// </summary>
    public async Task<IActionResult> OnPostProcessPaymentAsync()
    {
        try
        {
            // Retrieve cart information
            if (TempData["CartTotal"] is not decimal total)
            {
                _logger.LogError("Payment processing failed - no cart data");
                return RedirectToPage("/Index");
            }

            Amount = total;
            CustomerId = TempData["CustomerId"] as int?;

            // Generate unique transaction ID
            var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            _logger.LogInformation(
                "Initiating payment - TransactionId: {TransactionId}, Amount: {Amount}, Customer: {CustomerId}",
                transactionId, Amount, CustomerId);

            // Create payment request
            var paymentRequest = new MagnetiPaymentRequest
            {
                TransactionId = transactionId,
                Amount = Amount,
                Currency = "AED",
                Description = $"Self Checkout Purchase - {TempData["CartItemCount"]} items",
                CustomerId = CustomerId
            };

            // Process payment through Magneti
            var response = await _paymentService.ProcessPaymentAsync(paymentRequest);

            // Store payment result in TempData for success/failure pages
            TempData["PaymentTransactionId"] = response.TransactionId;
            TempData["PaymentAmount"] = response.Amount;
            TempData["PaymentAuthCode"] = response.AuthorizationCode;
            TempData["PaymentReferenceNumber"] = response.ReferenceNumber;
            TempData["PaymentCardType"] = response.CardType;
            TempData["PaymentCardLast4"] = response.CardLast4;

            if (response.Success && response.Status == PaymentStatus.Approved)
            {
                _logger.LogInformation(
                    "Payment approved - TransactionId: {TransactionId}, AuthCode: {AuthCode}",
                    response.TransactionId, response.AuthorizationCode);

                return RedirectToPage("/Success");
            }
            else
            {
                _logger.LogWarning(
                    "Payment failed - TransactionId: {TransactionId}, Status: {Status}, Error: {Error}",
                    response.TransactionId, response.Status, response.ErrorMessage);

                TempData["PaymentError"] = response.ErrorMessage ?? "Payment was declined";
                ErrorMessage = response.ErrorMessage ?? "Payment was declined. Please try again.";
                
                // Keep cart data for retry
                TempData.Keep();
                
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            ErrorMessage = "An error occurred processing your payment. Please try again.";
            TempData.Keep();
            return Page();
        }
    }

    /// <summary>
    /// Check payment status (for polling from JavaScript)
    /// </summary>
    public async Task<IActionResult> OnGetCheckStatusAsync(string transactionId)
    {
        try
        {
            var response = await _paymentService.CheckPaymentStatusAsync(transactionId);
            
            return new JsonResult(new
            {
                success = response.Success,
                status = response.Status.ToString(),
                authorizationCode = response.AuthorizationCode,
                errorMessage = response.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for {TransactionId}", transactionId);
            return new JsonResult(new { success = false, error = "Status check failed" });
        }
    }
}
