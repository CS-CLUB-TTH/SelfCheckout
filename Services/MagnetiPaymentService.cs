using System.Text;
using System.Text.Json;
using SelfCheckoutKiosk.Models;

namespace SelfCheckoutKiosk.Services;

/// <summary>
/// Implementation of Magneti payment gateway integration
/// This service handles communication with Magneti's API endpoints
/// </summary>
public class MagnetiPaymentService : IMagnetiPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MagnetiPaymentService> _logger;
    private readonly HttpClient _httpClient;

    // Configuration keys
    private const string ApiBaseUrlKey = "Magneti:ApiBaseUrl";
    private const string ApiKeyKey = "Magneti:ApiKey";
    private const string MerchantIdKey = "Magneti:MerchantId";
    private const string TerminalIdKey = "Magneti:TerminalId";

    public MagnetiPaymentService(
        IConfiguration configuration,
        ILogger<MagnetiPaymentService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("MagnetiPaymentClient");

        // Configure HttpClient base address and headers
        var baseUrl = _configuration[ApiBaseUrlKey] ?? "https://sandbox.mgipayments.com/api";
        _httpClient.BaseAddress = new Uri(baseUrl);
        
        var apiKey = _configuration[ApiKeyKey];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
    }

    /// <summary>
    /// Process a payment through Magneti gateway
    /// </summary>
    public async Task<MagnetiPaymentResponse> ProcessPaymentAsync(MagnetiPaymentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment - TransactionId: {TransactionId}, Amount: {Amount} {Currency}",
                request.TransactionId, request.Amount, request.Currency);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.TransactionId))
            {
                return CreateErrorResponse("Transaction ID is required", "INVALID_REQUEST");
            }

            if (request.Amount <= 0)
            {
                return CreateErrorResponse("Amount must be greater than zero", "INVALID_AMOUNT");
            }

            // Build API request payload
            var payload = new
            {
                merchant_id = _configuration[MerchantIdKey],
                terminal_id = _configuration[TerminalIdKey],
                transaction_id = request.TransactionId,
                amount = request.Amount.ToString("F2"),
                currency = request.Currency,
                description = request.Description ?? "Self Checkout Purchase",
                customer_id = request.CustomerId?.ToString(),
                timestamp = request.Timestamp.ToString("o")
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Call Magneti API endpoint
            var response = await _httpClient.PostAsync("/v1/transactions/process", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<MagnetiApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse != null)
                {
                    var paymentResponse = MapToPaymentResponse(apiResponse, request);
                    
                    _logger.LogInformation(
                        "Payment processed - TransactionId: {TransactionId}, Status: {Status}, AuthCode: {AuthCode}",
                        paymentResponse.TransactionId, paymentResponse.Status, paymentResponse.AuthorizationCode);
                    
                    return paymentResponse;
                }
            }

            // Handle API errors
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Payment API error - Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);

            return CreateErrorResponse(
                $"Payment gateway error: {response.ReasonPhrase}",
                response.StatusCode.ToString());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error processing payment for transaction {TransactionId}", request.TransactionId);
            return CreateErrorResponse("Unable to connect to payment gateway", "CONNECTION_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for transaction {TransactionId}", request.TransactionId);
            return CreateErrorResponse("An unexpected error occurred", "PROCESSING_ERROR");
        }
    }

    /// <summary>
    /// Check the status of an existing transaction
    /// </summary>
    public async Task<MagnetiPaymentResponse> CheckPaymentStatusAsync(string transactionId)
    {
        try
        {
            _logger.LogInformation("Checking payment status for transaction {TransactionId}", transactionId);

            var response = await _httpClient.GetAsync($"/v1/transactions/{transactionId}/status");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<MagnetiApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse != null)
                {
                    return MapToPaymentResponse(apiResponse, null);
                }
            }

            return CreateErrorResponse("Unable to retrieve transaction status", "STATUS_CHECK_FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for transaction {TransactionId}", transactionId);
            return CreateErrorResponse("Error checking payment status", "STATUS_CHECK_ERROR");
        }
    }

    /// <summary>
    /// Refund a transaction (full or partial)
    /// </summary>
    public async Task<MagnetiPaymentResponse> RefundPaymentAsync(string transactionId, decimal? amount = null)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund for transaction {TransactionId}, Amount: {Amount}",
                transactionId, amount?.ToString() ?? "Full");

            var payload = new
            {
                transaction_id = transactionId,
                amount = amount?.ToString("F2"),
                refund_type = amount.HasValue ? "partial" : "full"
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v1/transactions/{transactionId}/refund", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<MagnetiApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse != null)
                {
                    return MapToPaymentResponse(apiResponse, null);
                }
            }

            return CreateErrorResponse("Refund processing failed", "REFUND_FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return CreateErrorResponse("Error processing refund", "REFUND_ERROR");
        }
    }

    /// <summary>
    /// Map Magneti API response to our payment response model
    /// </summary>
    private MagnetiPaymentResponse MapToPaymentResponse(MagnetiApiResponse apiResponse, MagnetiPaymentRequest? request)
    {
        var status = ParsePaymentStatus(apiResponse.Status);
        
        return new MagnetiPaymentResponse
        {
            Success = status == PaymentStatus.Approved,
            Status = status,
            TransactionId = apiResponse.TransactionId ?? request?.TransactionId ?? string.Empty,
            AuthorizationCode = apiResponse.AuthorizationCode,
            ReferenceNumber = apiResponse.ReferenceNumber,
            Amount = decimal.TryParse(apiResponse.Amount, out var amt) ? amt : request?.Amount ?? 0,
            Currency = apiResponse.Currency ?? request?.Currency ?? "AED",
            ErrorMessage = apiResponse.ErrorMessage,
            ErrorCode = apiResponse.ErrorCode,
            CardType = apiResponse.CardType,
            CardLast4 = apiResponse.CardLast4,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Parse payment status from API response
    /// </summary>
    private PaymentStatus ParsePaymentStatus(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "APPROVED" or "SUCCESS" or "COMPLETED" => PaymentStatus.Approved,
            "DECLINED" or "REJECTED" or "FAILED" => PaymentStatus.Declined,
            "PENDING" or "PROCESSING" => PaymentStatus.Pending,
            "TIMEOUT" => PaymentStatus.Timeout,
            "CANCELLED" or "CANCELED" or "VOID" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Error
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    private MagnetiPaymentResponse CreateErrorResponse(string errorMessage, string errorCode)
    {
        return new MagnetiPaymentResponse
        {
            Success = false,
            Status = PaymentStatus.Error,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Internal model for Magneti API response
    /// </summary>
    private class MagnetiApiResponse
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
}
