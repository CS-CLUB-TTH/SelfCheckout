namespace SelfCheckoutKiosk.Services;

using SelfCheckoutKiosk.Models;

/// <summary>
/// Service for generating and formatting receipts
/// </summary>
public interface IReceiptService
{
    /// <summary>
    /// Generates ZPL (Zebra Programming Language) commands for thermal receipt printing
    /// This is the recommended format for Zebra printers via the Android WebView app
    /// </summary>
    /// <param name="receipt">Receipt data to print</param>
    /// <returns>ZPL command string</returns>
    string GenerateZplReceipt(Receipt receipt);

    /// <summary>
    /// Generates HTML receipt for browser printing fallback (testing/desktop)
    /// </summary>
    /// <param name="receipt">Receipt data to print</param>
    /// <returns>HTML string</returns>
    string GenerateHtmlReceipt(Receipt receipt);

    /// <summary>
    /// Generates plain text receipt
    /// </summary>
    /// <param name="receipt">Receipt data to print</param>
    /// <returns>Plain text string</returns>
    string GeneratePlainTextReceipt(Receipt receipt);

    /// <summary>
    /// Generates ESC/POS commands for thermal receipt printing
    /// Alternative format for non-Zebra printers via Android WebView app
    /// </summary>
    /// <param name="receipt">Receipt data to print</param>
    /// <returns>ESC/POS command string</returns>
    string GenerateEscPosReceipt(Receipt receipt);
}
