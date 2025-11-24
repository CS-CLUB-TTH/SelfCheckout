namespace SelfCheckoutKiosk.Services;

using SelfCheckoutKiosk.Models;
using System.Text;

/// <summary>
/// Service implementation for generating receipts in various formats
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly ILogger<ReceiptService> _logger;
    private readonly IConfiguration _configuration;

    public ReceiptService(ILogger<ReceiptService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Generates ZPL (Zebra Programming Language) commands for thermal receipt printing
    /// ZPL is optimized for Zebra printers and provides fast, high-quality thermal printing
    /// </summary>
    public string GenerateZplReceipt(Receipt receipt)
    {
        try
        {
            var zpl = new StringBuilder();

            // Start ZPL script
            zpl.AppendLine("^XA"); // Start format

            // Set label width and orientation
            zpl.AppendLine("^PW400"); // Print width (400 dots = ~2.8 inches for 58mm paper)
            zpl.AppendLine("^LL0"); // Auto-calculate label length

            var yPosition = 10; // Starting Y position

            // Store name (centered, large font)
            zpl.AppendLine($"^FO50,{yPosition}^A0N,40,40^FD{receipt.StoreName}^FS");
            yPosition += 50;

            // Separator line
            zpl.AppendLine($"^FO10,{yPosition}^GB380,2,2^FS");
            yPosition += 15;

            // Transaction date and time
            var dateStr = receipt.TransactionDate.ToString("dd/MM/yyyy HH:mm:ss");
            zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDDate: {dateStr}^FS");
            yPosition += 30;

            // Transaction ID
            zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDTransaction: {receipt.TransactionId}^FS");
            yPosition += 30;

            if (receipt.CustomerId.HasValue)
            {
                zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDCustomer: {receipt.CustomerId}^FS");
                yPosition += 30;
            }

            // Separator line
            zpl.AppendLine($"^FO10,{yPosition}^GB380,2,2^FS");
            yPosition += 15;

            // Items header
            zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDItem^FS");
            zpl.AppendLine($"^FO200,{yPosition}^A0N,20,20^FDQty^FS");
            zpl.AppendLine($"^FO280,{yPosition}^A0N,20,20^FDAmount^FS");
            yPosition += 30;

            // Line under header
            zpl.AppendLine($"^FO10,{yPosition}^GB380,1,1^FS");
            yPosition += 10;

            // Items
            foreach (var item in receipt.Items)
            {
                // Item description (may wrap if too long)
                var desc = item.Description.Length > 25 ? item.Description.Substring(0, 22) + "..." : item.Description;
                zpl.AppendLine($"^FO10,{yPosition}^A0N,18,18^FD{desc}^FS");
                
                // Quantity
                var qtyStr = item.Quantity.ToString("0.##");
                zpl.AppendLine($"^FO200,{yPosition}^A0N,18,18^FD{qtyStr}^FS");
                
                // Amount
                var amtStr = item.Amount.ToString("0.00");
                zpl.AppendLine($"^FO280,{yPosition}^A0N,18,18^FD{amtStr}^FS");
                
                yPosition += 25;
            }

            yPosition += 10;

            // Separator line
            zpl.AppendLine($"^FO10,{yPosition}^GB380,2,2^FS");
            yPosition += 15;

            // Subtotal
            zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDSubtotal:^FS");
            zpl.AppendLine($"^FO280,{yPosition}^A0N,20,20^FD{receipt.Subtotal.ToString("0.00")} {receipt.Currency}^FS");
            yPosition += 30;

            // Tax
            zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDTax (VAT):^FS");
            zpl.AppendLine($"^FO280,{yPosition}^A0N,20,20^FD{receipt.Tax.ToString("0.00")} {receipt.Currency}^FS");
            yPosition += 30;

            // Line before total
            zpl.AppendLine($"^FO10,{yPosition}^GB380,2,2^FS");
            yPosition += 15;

            // Total (bold/larger)
            zpl.AppendLine($"^FO10,{yPosition}^A0N,30,30^FDTOTAL:^FS");
            zpl.AppendLine($"^FO280,{yPosition}^A0N,30,30^FD{receipt.Total.ToString("0.00")} {receipt.Currency}^FS");
            yPosition += 40;

            // Separator line
            zpl.AppendLine($"^FO10,{yPosition}^GB380,2,2^FS");
            yPosition += 15;

            // Payment details
            if (!string.IsNullOrEmpty(receipt.CardType) && !string.IsNullOrEmpty(receipt.CardLast4))
            {
                zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDCard: {receipt.CardType} ****{receipt.CardLast4}^FS");
                yPosition += 30;
            }

            if (!string.IsNullOrEmpty(receipt.AuthorizationCode))
            {
                zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDAuth Code: {receipt.AuthorizationCode}^FS");
                yPosition += 30;
            }

            if (!string.IsNullOrEmpty(receipt.ReferenceNumber))
            {
                zpl.AppendLine($"^FO10,{yPosition}^A0N,20,20^FDRef: {receipt.ReferenceNumber}^FS");
                yPosition += 30;
            }

            yPosition += 20;

            // Footer message
            zpl.AppendLine($"^FO50,{yPosition}^A0N,22,22^FDThank you for shopping!^FS");
            yPosition += 40;

            // End ZPL script
            zpl.AppendLine("^XZ"); // End format

            _logger.LogInformation("Generated ZPL receipt for transaction {TransactionId}", receipt.TransactionId);
            return zpl.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ZPL receipt for transaction {TransactionId}", receipt.TransactionId);
            throw;
        }
    }

    /// <summary>
    /// Generates HTML receipt for browser printing fallback
    /// </summary>
    public string GenerateHtmlReceipt(Receipt receipt)
    {
        try
        {
            var html = new StringBuilder();

            html.AppendLine("<html><head>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: 'Courier New', monospace; width: 300px; margin: 0 auto; padding: 20px; }");
            html.AppendLine(".center { text-align: center; }");
            html.AppendLine(".bold { font-weight: bold; }");
            html.AppendLine(".large { font-size: 18px; }");
            html.AppendLine(".line { border-top: 1px dashed #000; margin: 10px 0; }");
            html.AppendLine(".item-row { display: flex; justify-content: space-between; margin: 5px 0; }");
            html.AppendLine(".total-row { display: flex; justify-content: space-between; margin: 10px 0; font-size: 16px; font-weight: bold; }");
            html.AppendLine("@media print { body { padding: 0; } }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            // Header
            html.AppendLine($"<div class='center bold large'>{receipt.StoreName}</div>");
            html.AppendLine("<div class='line'></div>");

            // Transaction info
            html.AppendLine($"<div>Date: {receipt.TransactionDate:dd/MM/yyyy HH:mm:ss}</div>");
            html.AppendLine($"<div>Transaction: {receipt.TransactionId}</div>");
            if (receipt.CustomerId.HasValue)
            {
                html.AppendLine($"<div>Customer: {receipt.CustomerId}</div>");
            }

            html.AppendLine("<div class='line'></div>");

            // Items
            foreach (var item in receipt.Items)
            {
                html.AppendLine("<div class='item-row'>");
                html.AppendLine($"<span>{item.Description}</span>");
                html.AppendLine($"<span>{item.Quantity:0.##} x {item.Price:0.00}</span>");
                html.AppendLine("</div>");
                html.AppendLine($"<div class='item-row' style='padding-left: 20px;'>");
                html.AppendLine($"<span></span><span>{item.Amount:0.00} {receipt.Currency}</span>");
                html.AppendLine("</div>");
            }

            html.AppendLine("<div class='line'></div>");

            // Totals
            html.AppendLine($"<div class='item-row'><span>Subtotal:</span><span>{receipt.Subtotal:0.00} {receipt.Currency}</span></div>");
            html.AppendLine($"<div class='item-row'><span>Tax (VAT):</span><span>{receipt.Tax:0.00} {receipt.Currency}</span></div>");
            html.AppendLine("<div class='line'></div>");
            html.AppendLine($"<div class='total-row'><span>TOTAL:</span><span>{receipt.Total:0.00} {receipt.Currency}</span></div>");

            html.AppendLine("<div class='line'></div>");

            // Payment details
            if (!string.IsNullOrEmpty(receipt.CardType) && !string.IsNullOrEmpty(receipt.CardLast4))
            {
                html.AppendLine($"<div>Card: {receipt.CardType} ****{receipt.CardLast4}</div>");
            }
            if (!string.IsNullOrEmpty(receipt.AuthorizationCode))
            {
                html.AppendLine($"<div>Auth Code: {receipt.AuthorizationCode}</div>");
            }
            if (!string.IsNullOrEmpty(receipt.ReferenceNumber))
            {
                html.AppendLine($"<div>Reference: {receipt.ReferenceNumber}</div>");
            }

            html.AppendLine("<br/>");
            html.AppendLine("<div class='center'>Thank you for shopping!</div>");

            html.AppendLine("</body></html>");

            _logger.LogInformation("Generated HTML receipt for transaction {TransactionId}", receipt.TransactionId);
            return html.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HTML receipt for transaction {TransactionId}", receipt.TransactionId);
            throw;
        }
    }

    /// <summary>
    /// Generates plain text receipt
    /// </summary>
    public string GeneratePlainTextReceipt(Receipt receipt)
    {
        try
        {
            var text = new StringBuilder();

            text.AppendLine("================================");
            text.AppendLine(receipt.StoreName.PadLeft((32 + receipt.StoreName.Length) / 2));
            text.AppendLine("================================");
            text.AppendLine();
            text.AppendLine($"Date: {receipt.TransactionDate:dd/MM/yyyy HH:mm:ss}");
            text.AppendLine($"Transaction: {receipt.TransactionId}");
            if (receipt.CustomerId.HasValue)
            {
                text.AppendLine($"Customer: {receipt.CustomerId}");
            }
            text.AppendLine();
            text.AppendLine("--------------------------------");
            text.AppendLine($"{"Item",-20} {"Qty",5} {"Amount",10}");
            text.AppendLine("--------------------------------");

            foreach (var item in receipt.Items)
            {
                text.AppendLine($"{item.Description,-20} {item.Quantity,5:0.##} {item.Amount,10:0.00}");
            }

            text.AppendLine("--------------------------------");
            text.AppendLine($"{"Subtotal:",-26} {receipt.Subtotal,10:0.00}");
            text.AppendLine($"{"Tax (VAT):",-26} {receipt.Tax,10:0.00}");
            text.AppendLine("================================");
            text.AppendLine($"{"TOTAL:",-26} {receipt.Total,10:0.00} {receipt.Currency}");
            text.AppendLine("================================");
            text.AppendLine();

            if (!string.IsNullOrEmpty(receipt.CardType) && !string.IsNullOrEmpty(receipt.CardLast4))
            {
                text.AppendLine($"Card: {receipt.CardType} ****{receipt.CardLast4}");
            }
            if (!string.IsNullOrEmpty(receipt.AuthorizationCode))
            {
                text.AppendLine($"Auth Code: {receipt.AuthorizationCode}");
            }
            if (!string.IsNullOrEmpty(receipt.ReferenceNumber))
            {
                text.AppendLine($"Reference: {receipt.ReferenceNumber}");
            }

            text.AppendLine();
            text.AppendLine("   Thank you for shopping!   ");
            text.AppendLine();

            _logger.LogInformation("Generated plain text receipt for transaction {TransactionId}", receipt.TransactionId);
            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating plain text receipt for transaction {TransactionId}", receipt.TransactionId);
            throw;
        }
    }
}
