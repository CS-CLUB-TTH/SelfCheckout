using Microsoft.AspNetCore.Mvc.RazorPages;
using SelfCheckoutKiosk.Models;
using SelfCheckoutKiosk.Services;
using System.Globalization;
using System.Text.Json;

namespace SelfCheckoutKiosk.Pages;

public class SuccessModel : PageModel
{
    private readonly IReceiptService _receiptService;
    private readonly ILogger<SuccessModel> _logger;

    public SuccessModel(IReceiptService receiptService, ILogger<SuccessModel> logger)
    {
        _receiptService = receiptService;
        _logger = logger;
    }

    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CardType { get; set; }
    public string? CardLast4 { get; set; }
    public string? ReceiptZpl { get; set; }
    public string? ReceiptHtml { get; set; }
    public string? ReceiptJson { get; set; }

    public void OnGet()
    {
        // Retrieve payment details from TempData
        TransactionId = TempData["PaymentTransactionId"] as string;
        
        // Handle Amount stored as string
        if (TempData["PaymentAmount"] is string amountStr 
            && decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            Amount = amount;
        }
        else
        {
            Amount = TempData["PaymentAmount"] as decimal? ?? 0;
        }
        
        AuthorizationCode = TempData["PaymentAuthCode"] as string;
        ReferenceNumber = TempData["PaymentReferenceNumber"] as string;
        CardType = TempData["PaymentCardType"] as string;
        CardLast4 = TempData["PaymentCardLast4"] as string;

        // Retrieve cart items and totals for receipt
        var subtotal = 0m;
        var tax = 0m;
        var items = new List<ReceiptItem>();

        if (TempData["CartSubtotal"] is string subtotalStr 
            && decimal.TryParse(subtotalStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var sub))
        {
            subtotal = sub;
        }

        if (TempData["CartTax"] is string taxStr 
            && decimal.TryParse(taxStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var t))
        {
            tax = t;
        }

        // Try to retrieve cart items from TempData
        if (TempData["CartItems"] is string cartItemsJson)
        {
            try
            {
                var cartItems = JsonSerializer.Deserialize<List<ReceiptItem>>(cartItemsJson);
                if (cartItems != null)
                {
                    items = cartItems;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cart items for receipt");
            }
        }

        // Create receipt object
        var receipt = new Receipt
        {
            TransactionId = TransactionId ?? "N/A",
            TransactionDate = DateTime.Now,
            StoreName = "Self Checkout Kiosk",
            CustomerId = TempData["CustomerId"] as int?,
            Items = items,
            Subtotal = subtotal,
            Tax = tax,
            Total = Amount,
            AuthorizationCode = AuthorizationCode,
            ReferenceNumber = ReferenceNumber,
            CardType = CardType,
            CardLast4 = CardLast4,
            Currency = "AED"
        };

        try
        {
            // Generate receipt in multiple formats
            ReceiptZpl = _receiptService.GenerateZplReceipt(receipt);
            ReceiptHtml = _receiptService.GenerateHtmlReceipt(receipt);
            ReceiptJson = JsonSerializer.Serialize(receipt);

            _logger.LogInformation("Receipt generated successfully for transaction {TransactionId}", TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate receipt for transaction {TransactionId}", TransactionId);
        }
    }
}
