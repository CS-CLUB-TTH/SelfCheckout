# Magneti Payment Integration Guide

This document provides comprehensive information about the Magneti (MGI Payments) integration in the Self Checkout Kiosk application.

## Overview

The application integrates with Magneti's payment gateway API to process card payments securely. The integration follows industry best practices for payment processing, including:

- Secure API authentication
- PCI-compliant payment handling
- Comprehensive error handling
- Transaction logging and tracking
- Status monitoring and retry capabilities

## Architecture

### Components

1. **Models** (`/Models`)
   - `MagnetiPaymentRequest.cs` - Request DTO for payment transactions
   - `MagnetiPaymentResponse.cs` - Response DTO with payment results
   - `PaymentStatus` enum - Transaction status enumeration

2. **Services** (`/Services`)
   - `IMagnetiPaymentService.cs` - Payment service interface
   - `MagnetiPaymentService.cs` - Magneti API integration implementation

3. **Pages** (`/Pages`)
   - `Cart.cshtml/.cs` - Prepares cart data and passes to payment
   - `Payment.cshtml/.cs` - Initiates and processes payments
   - `Success.cshtml/.cs` - Displays successful payment details

### Payment Flow

```
1. Customer scans NFC card → Cart loads items
   ↓
2. Customer clicks "Proceed to Payment"
   ↓
3. Cart page stores: Total, Subtotal, Tax, CustomerID in TempData
   ↓
4. Payment page loads with cart details
   ↓
5. Auto-submit payment form after 1 second
   ↓
6. PaymentModel.OnPostProcessPaymentAsync() called
   ↓
7. Generate unique transaction ID (TXN-{timestamp}-{guid})
   ↓
8. Create MagnetiPaymentRequest with amount and details
   ↓
9. Call MagnetiPaymentService.ProcessPaymentAsync()
   ↓
10. Service sends POST to Magneti API: /v1/transactions/process
    ↓
11. Receive MagnetiPaymentResponse
    ↓
12. If Approved → Redirect to Success page with transaction details
    If Declined/Error → Stay on Payment page with error message
```

## Configuration

### Required Settings

Add the following configuration to your `appsettings.json`:

```json
{
  "Magneti": {
    "ApiBaseUrl": "https://sandbox.mgipayments.com/api",
    "ApiKey": "YOUR_MAGNETI_API_KEY",
    "MerchantId": "YOUR_MERCHANT_ID",
    "TerminalId": "YOUR_TERMINAL_ID"
  }
}
```

### Configuration Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `ApiBaseUrl` | Magneti API endpoint URL | `https://sandbox.mgipayments.com/api` (sandbox)<br>`https://api.mgipayments.com/api` (production) |
| `ApiKey` | API authentication key from Magneti | `sk_test_abc123...` |
| `MerchantId` | Your merchant identifier | `MERCH-12345` |
| `TerminalId` | Terminal/POS identifier | `TERM-001` |

### Environment Variables (Recommended for Production)

For production deployments, use environment variables instead of storing credentials in `appsettings.json`:

**Linux/macOS:**
```bash
export Magneti__ApiKey="your-api-key"
export Magneti__MerchantId="your-merchant-id"
export Magneti__TerminalId="your-terminal-id"
```

**Windows:**
```cmd
set Magneti__ApiKey=your-api-key
set Magneti__MerchantId=your-merchant-id
set Magneti__TerminalId=your-terminal-id
```

**Azure App Service:**
```
Magneti:ApiKey = your-api-key
Magneti:MerchantId = your-merchant-id
Magneti:TerminalId = your-terminal-id
```

## API Integration Details

### Authentication

The service uses Bearer token authentication. The API key is sent in the Authorization header:

```
Authorization: Bearer {ApiKey}
```

### Endpoints

#### 1. Process Payment

**Endpoint:** `POST /v1/transactions/process`

**Request Payload:**
```json
{
  "merchant_id": "MERCH-12345",
  "terminal_id": "TERM-001",
  "transaction_id": "TXN-20250121123045-A1B2C3D4",
  "amount": "103.95",
  "currency": "AED",
  "description": "Self Checkout Purchase - 3 items",
  "customer_id": "12345",
  "timestamp": "2025-01-21T12:30:45.000Z"
}
```

**Response (Success):**
```json
{
  "status": "APPROVED",
  "transaction_id": "TXN-20250121123045-A1B2C3D4",
  "authorization_code": "AUTH123456",
  "reference_number": "MGI-REF-789012",
  "amount": "103.95",
  "currency": "AED",
  "card_type": "VISA",
  "card_last4": "4242"
}
```

**Response (Declined):**
```json
{
  "status": "DECLINED",
  "transaction_id": "TXN-20250121123045-A1B2C3D4",
  "error_code": "INSUFFICIENT_FUNDS",
  "error_message": "Insufficient funds"
}
```

#### 2. Check Payment Status

**Endpoint:** `GET /v1/transactions/{transactionId}/status`

**Response:**
```json
{
  "status": "APPROVED",
  "transaction_id": "TXN-20250121123045-A1B2C3D4",
  "authorization_code": "AUTH123456",
  "reference_number": "MGI-REF-789012"
}
```

#### 3. Refund Payment

**Endpoint:** `POST /v1/transactions/{transactionId}/refund`

**Request Payload (Partial Refund):**
```json
{
  "transaction_id": "TXN-20250121123045-A1B2C3D4",
  "amount": "50.00",
  "refund_type": "partial"
}
```

**Request Payload (Full Refund):**
```json
{
  "transaction_id": "TXN-20250121123045-A1B2C3D4",
  "refund_type": "full"
}
```

## Payment Status Codes

| Status | Description | Action |
|--------|-------------|--------|
| `APPROVED` / `SUCCESS` / `COMPLETED` | Payment was successful | Proceed to success page |
| `DECLINED` / `REJECTED` / `FAILED` | Payment was declined by bank | Show error, allow retry |
| `PENDING` / `PROCESSING` | Payment is being processed | Wait or poll for status |
| `TIMEOUT` | Request timed out | Retry or check status |
| `CANCELLED` / `VOID` | Transaction was cancelled | Return to cart |
| Other | Unknown/error status | Treat as error |

## Error Handling

### Error Response Structure

```csharp
{
  "Success": false,
  "Status": "Error",
  "ErrorMessage": "User-friendly error message",
  "ErrorCode": "TECHNICAL_ERROR_CODE",
  "Timestamp": "2025-01-21T12:30:45Z"
}
```

### Common Error Codes

| Error Code | Description | Resolution |
|------------|-------------|------------|
| `INVALID_REQUEST` | Missing required fields | Check transaction ID and amount |
| `INVALID_AMOUNT` | Amount is zero or negative | Verify cart total |
| `CONNECTION_ERROR` | Cannot connect to gateway | Check network/firewall, verify API URL |
| `PROCESSING_ERROR` | Unexpected error during processing | Check logs, contact support |
| `INSUFFICIENT_FUNDS` | Card has insufficient funds | Ask customer to use different card |
| `INVALID_CARD` | Card is invalid or expired | Ask customer to use different card |
| `STATUS_CHECK_FAILED` | Cannot retrieve transaction status | Retry or contact support |

## Security Considerations

### PCI Compliance

- ✅ **No card data storage**: The application never stores or processes raw card data
- ✅ **Secure transmission**: All API calls use HTTPS/TLS
- ✅ **Tokenization**: Magneti handles card tokenization
- ✅ **API authentication**: Bearer token authentication
- ✅ **Logging**: No sensitive data (card numbers, CVV) in logs

### Best Practices Implemented

1. **Credentials Security**
   - API keys stored in configuration (not in code)
   - Environment variables recommended for production
   - No credentials in source control

2. **Data Validation**
   - Transaction ID validation
   - Amount validation (positive, non-zero)
   - Input sanitization

3. **Error Handling**
   - Try-catch blocks around all API calls
   - Generic error messages to users (no technical details)
   - Detailed logging for troubleshooting

4. **Transaction Tracking**
   - Unique transaction IDs for each payment
   - Comprehensive logging (without sensitive data)
   - Transaction reference storage for reconciliation

## Testing

### Sandbox Testing

1. **Configure Sandbox Credentials**
   ```json
   {
     "Magneti": {
       "ApiBaseUrl": "https://sandbox.mgipayments.com/api",
       "ApiKey": "sk_test_YOUR_TEST_KEY",
       "MerchantId": "TEST_MERCHANT",
       "TerminalId": "TEST_TERMINAL"
     }
   }
   ```

2. **Test Cards** (obtain from Magneti documentation)
   - Successful payment: Use test card provided by Magneti
   - Declined payment: Use declined test card
   - Insufficient funds: Use specific test card

3. **Test Scenarios**
   - ✅ Successful payment flow
   - ✅ Declined payment (various reasons)
   - ✅ Network timeout
   - ✅ Invalid credentials
   - ✅ API errors
   - ✅ Status check
   - ✅ Refund processing

### Unit Testing (Future Enhancement)

Create unit tests for:
- Payment request validation
- Response mapping
- Error handling
- Status parsing

## Logging

### Log Levels

**Information (Info):**
- Payment initiation
- Successful transactions
- Status checks

**Warning:**
- Payment declined
- Validation errors

**Error:**
- API communication errors
- Unexpected exceptions
- Configuration issues

### Example Log Entries

```
[Information] Processing payment - TransactionId: TXN-20250121123045-A1B2C3D4, Amount: 103.95 AED, Customer: 12345

[Information] Payment approved - TransactionId: TXN-20250121123045-A1B2C3D4, AuthCode: AUTH123456

[Warning] Payment failed - TransactionId: TXN-20250121123045-A1B2C3D4, Status: Declined, Error: Insufficient funds

[Error] HTTP error processing payment for transaction TXN-20250121123045-A1B2C3D4
```

## Troubleshooting

### Common Issues

#### 1. "Unable to connect to payment gateway"

**Causes:**
- Network connectivity issues
- Firewall blocking outbound HTTPS
- Incorrect API base URL
- Magneti API downtime

**Solutions:**
- Verify internet connection
- Check firewall rules (allow HTTPS to Magneti domains)
- Verify `ApiBaseUrl` in configuration
- Check Magneti status page

#### 2. "Payment gateway error: Unauthorized"

**Causes:**
- Invalid API key
- Expired API key
- Wrong environment (test key in production)

**Solutions:**
- Verify API key in configuration
- Regenerate API key in Magneti dashboard
- Ensure using correct environment credentials

#### 3. "Payment processing failed"

**Causes:**
- Invalid merchant ID or terminal ID
- Missing required fields
- API version mismatch

**Solutions:**
- Verify merchant ID and terminal ID
- Check API documentation for required fields
- Ensure using correct API version

#### 4. Transaction stuck in "Pending"

**Solutions:**
- Use `CheckPaymentStatusAsync()` to query status
- Wait 30-60 seconds and retry
- Contact Magneti support with transaction ID

## Production Deployment Checklist

- [ ] Replace sandbox credentials with production credentials
- [ ] Update `ApiBaseUrl` to production endpoint
- [ ] Store credentials in environment variables (not appsettings.json)
- [ ] Enable HTTPS/SSL on application
- [ ] Configure proper logging (Application Insights, etc.)
- [ ] Set up monitoring and alerts
- [ ] Test with real cards in controlled environment
- [ ] Verify network connectivity to Magneti production API
- [ ] Configure proper error pages
- [ ] Set up transaction reconciliation process
- [ ] Train staff on handling payment issues
- [ ] Document support contact information

## API Reference Documentation

For detailed API documentation, visit:
- **Magneti Developer Portal:** https://sandbox.mgipayments.com/docs/api/
- **Magneti Support:** Contact your Magneti account manager

## Support

### Application Issues
Contact the development team with:
- Transaction ID
- Timestamp
- Error message
- Application logs

### Magneti Gateway Issues
Contact Magneti support with:
- Merchant ID
- Transaction ID
- Reference number (if available)
- Error code
- Timestamp

## Future Enhancements

Potential improvements for the payment integration:

1. **Webhooks** - Real-time payment notifications
2. **Retry Logic** - Automatic retry on transient failures
3. **Receipt Printing** - Print payment receipts
4. **Refund UI** - Admin interface for processing refunds
5. **Analytics** - Payment success rate tracking
6. **Multi-currency** - Support for multiple currencies
7. **Split Payments** - Split payment across multiple cards
8. **Saved Cards** - Tokenized card storage (with PCI compliance)
9. **QR Code Payments** - Mobile wallet integration
10. **3D Secure** - Enhanced security for online payments

## Changelog

### Version 1.0.0 (2025-01-21)
- ✅ Initial Magneti payment integration
- ✅ Payment processing with status handling
- ✅ Transaction tracking and logging
- ✅ Error handling and user feedback
- ✅ Refund capability (API only)
- ✅ Configuration management
- ✅ Documentation

---

**Document Version:** 1.0.0  
**Last Updated:** 2025-01-21  
**Author:** Development Team
