using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SelfCheckoutKiosk.Models;
using SelfCheckoutKiosk.Services;

namespace SelfCheckoutKiosk.Pages;

public class CartModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<CartModel> _logger;

    public CartModel(IDatabaseService databaseService, ILogger<CartModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public List<BillDetail> CartItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string? ErrorMessage { get; set; }
    public int? CustomerId { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetLoadFromCardAsync(string cardNo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cardNo))
            {
                ErrorMessage = "Invalid card number";
                return Page();
            }

            _logger.LogInformation("Loading cart for card: {CardNo}", cardNo);

            // Step 1: Get customer key from card number
            var cusKey = await _databaseService.GetCustomerKeyByCardNo(cardNo);

            if (cusKey == null)
            {
                _logger.LogWarning("No customer found for card: {CardNo}", cardNo);
                ErrorMessage = "No customer found with this card. Please contact staff.";
                return Page();
            }

            _logger.LogInformation("Found customer key: {CusKey} for card: {CardNo}", cusKey, cardNo);

            // Step 2: Get bill details using the stored procedure
            CartItems = await _databaseService.GetBillDetailsByCustomerKey(cusKey.Value);

            if (CartItems.Count == 0)
            {
                _logger.LogInformation("No items found for customer key: {CusKey}", cusKey);
                ErrorMessage = "No items found in your cart. Please add items first.";
                return Page();
            }

            // Calculate totals
            // Amount includes item cost minus discounts, VatAmt is the VAT
            Subtotal = CartItems.Sum(item => item.Amount);
            Tax = CartItems.Sum(item => item.VatAmt);
            Total = Subtotal + Tax; // Total = subtotal + tax

            // Store cart information in TempData for payment processing
            CustomerId = cusKey.Value;
            TempData["CustomerId"] = CustomerId;
            TempData["CartTotal"] = Total;
            TempData["CartSubtotal"] = Subtotal;
            TempData["CartTax"] = Tax;
            TempData["CartItemCount"] = CartItems.Count;

            _logger.LogInformation("Loaded {Count} items for customer {CusKey}, Total: {Total}", 
                CartItems.Count, cusKey, Total);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cart for card: {CardNo}", cardNo);
            ErrorMessage = "An error occurred loading your cart. Please try again or contact staff.";
            return Page();
        }
    }
}

