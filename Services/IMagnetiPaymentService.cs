using SelfCheckoutKiosk.Models;

namespace SelfCheckoutKiosk.Services;

/// <summary>
/// Service for processing payments through Magneti payment gateway
/// </summary>
public interface IMagnetiPaymentService
{
    /// <summary>
    /// Initialize a payment transaction
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment response with status and transaction details</returns>
    Task<MagnetiPaymentResponse> ProcessPaymentAsync(MagnetiPaymentRequest request);

    /// <summary>
    /// Check the status of a payment transaction
    /// </summary>
    /// <param name="transactionId">Transaction ID to check</param>
    /// <returns>Current payment status</returns>
    Task<MagnetiPaymentResponse> CheckPaymentStatusAsync(string transactionId);

    /// <summary>
    /// Refund a completed transaction (optional - for future use)
    /// </summary>
    /// <param name="transactionId">Original transaction ID</param>
    /// <param name="amount">Amount to refund (null for full refund)</param>
    /// <returns>Refund response</returns>
    Task<MagnetiPaymentResponse> RefundPaymentAsync(string transactionId, decimal? amount = null);
}
