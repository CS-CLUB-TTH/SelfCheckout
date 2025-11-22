# Magneti Payment Integration - Configuration Guide

## Overview

The Self Checkout Kiosk supports **TWO payment modes**:

1. **Simulation Mode (DEFAULT)** ✅ Recommended for Testing
2. **REST API Mode** (requires Magneti API credentials)

**NO DLL or Physical Terminal Required!**

## Quick Start (Simulation Mode)

The application works **OUT OF THE BOX** in simulation mode - no configuration needed!

### Current Configuration (`appsettings.json`)

```json
{
  "Magneti": {
    "UseSimulation": true,    // ✅ Simulation mode enabled
    "ApiBaseUrl": "https://sandbox.mgipayments.com/api",
    "ApiKey": "",              // Not needed for simulation
    "MerchantId": "MERCHANT001",
    "TerminalId": "TERMINAL001"
  }
}
```

### What You Get with Simulation Mode

✅ All payment flows work perfectly  
✅ Simulated approved transactions  
✅ Realistic payment processing (1.5s delay)  
✅ Transaction IDs, auth codes, reference numbers  
✅ No external dependencies  
✅ Perfect for development and testing  

## Switching to REST API Mode

When you're ready to use real Magneti API:

### Step 1: Get Magneti Credentials

Contact Magneti to obtain:
- API Key
- Merchant ID
- Terminal ID

### Step 2: Update Configuration

```json
{
  "Magneti": {
    "UseSimulation": false,    // ⚠️ Disable simulation
    "ApiBaseUrl": "https://sandbox.mgipayments.com/api",  // or production URL
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE",
    "MerchantId": "YOUR_MERCHANT_ID",
    "TerminalId": "YOUR_TERMINAL_ID"
  }
}
```

### Step 3: Test with Sandbox

Use Magneti's sandbox environment first:
- URL: `https://sandbox.mgipayments.com/api`
- Test with sandbox credentials
- Verify all flows work correctly

### Step 4: Production Deployment

For production:
1. Update `ApiBaseUrl` to production endpoint
2. Use production credentials
3. Store credentials in environment variables (more secure):

**Linux/macOS:**
```bash
export Magneti__ApiKey="your-production-key"
export Magneti__MerchantId="your-merchant-id"
export Magneti__TerminalId="your-terminal-id"
export Magneti__UseSimulation="false"
```

**Windows:**
```cmd
set Magneti__ApiKey=your-production-key
set Magneti__MerchantId=your-merchant-id
set Magneti__TerminalId=your-terminal-id
set Magneti__UseSimulation=false
```

## Payment Flow

### How It Works (Both Modes)

1. **Customer scans NFC card** → Cart loads with items
2. **Customer clicks "Proceed to Payment"** → Redirects to Payment page
3. **Payment page auto-submits** after 1 second
4. **Payment processes:**
   - **Simulation Mode**: Generates simulated approval
   - **REST API Mode**: Calls Magneti API
5. **On Success**: Shows success page with transaction details
6. **On Failure**: Shows error, allows retry

### Sample Simulation Response

```json
{
  "Success": true,
  "Status": "Approved",
  "TransactionId": "TXN-20250122143045-A1B2C3D4",
  "AuthorizationCode": "SIM143045",
  "ReferenceNumber": "REF20250122143045",
  "Amount": 103.95,
  "Currency": "AED",
  "CardType": "VISA",
  "CardLast4": "1234"
}
```

## Testing Checklist

### Simulation Mode Testing
- [x] Navigate to NFC page
- [x] Simulate card tap (or skip to cart directly)
- [x] Load cart with items
- [x] Click "Proceed to Payment"
- [x] Verify payment processes
- [x] Check success page displays correctly
- [x] Verify transaction details shown

### REST API Mode Testing
- [ ] Configure Magneti credentials
- [ ] Test successful payment
- [ ] Test declined payment (use test card)
- [ ] Test timeout scenario
- [ ] Test network error handling
- [ ] Verify API logs

## Troubleshooting

### Issue: "Using simulation mode" message in logs

**Cause**: Either `UseSimulation` is `true` OR `ApiKey` is empty

**Solution**: 
- For testing: This is correct, keep using simulation
- For production: Set `UseSimulation` to `false` and provide valid `ApiKey`

### Issue: "Unable to connect to payment gateway"

**Cause**: Network/firewall blocking Magneti API

**Solution**:
1. Check internet connection
2. Verify firewall allows HTTPS to Magneti domain
3. Test API URL with curl:
   ```bash
   curl -v https://sandbox.mgipayments.com/api/health
   ```

### Issue: "Payment gateway error: Unauthorized"

**Cause**: Invalid API key

**Solution**:
1. Verify API key in configuration
2. Regenerate API key in Magneti dashboard
3. Ensure using correct environment (sandbox vs production)

## Security Best Practices

### ✅ DO:
- Use simulation mode for development/testing
- Store API credentials in environment variables (production)
- Use HTTPS for all communications
- Log transactions (without sensitive data)
- Rotate API keys regularly

### ❌ DON'T:
- Commit real API keys to source control
- Use production keys in development
- Log card numbers or CVV codes
- Disable HTTPS in production

## Configuration Reference

### Magneti Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `UseSimulation` | boolean | `true` | Enable simulation mode |
| `ApiBaseUrl` | string | Sandbox URL | Magneti API endpoint |
| `ApiKey` | string | Empty | API authentication key |
| `MerchantId` | string | "MERCHANT001" | Your merchant identifier |
| `TerminalId` | string | "TERMINAL001" | Terminal/POS identifier |

### Environment-Specific Configuration

**Development (`appsettings.Development.json`):**
```json
{
  "Magneti": {
    "UseSimulation": true
  }
}
```

**Staging:**
```json
{
  "Magneti": {
    "UseSimulation": false,
    "ApiBaseUrl": "https://sandbox.mgipayments.com/api",
    "ApiKey": "sandbox_key_here"
  }
}
```

**Production:**
```json
{
  "Magneti": {
    "UseSimulation": false,
    "ApiBaseUrl": "https://api.mgipayments.com/api"
  }
}
```
(Store ApiKey in environment variables)

## API Endpoints Used

When in REST API mode, the service calls:

### Process Payment
```
POST /v1/transactions/process
Authorization: Bearer {ApiKey}
Content-Type: application/json

{
  "merchant_id": "...",
  "terminal_id": "...",
  "transaction_id": "...",
  "amount": "103.95",
  "currency": "AED",
  "description": "Self Checkout Purchase - 3 items",
  "customer_id": "12345",
  "timestamp": "2025-01-22T14:30:45.000Z"
}
```

### Check Payment Status
```
GET /v1/transactions/{transactionId}/status
Authorization: Bearer {ApiKey}
```

### Process Refund
```
POST /v1/transactions/{transactionId}/refund
Authorization: Bearer {ApiKey}
Content-Type: application/json

{
  "transaction_id": "...",
  "amount": "50.00",
  "refund_type": "partial"
}
```

## Summary

### For Development & Testing:
✅ **Use Simulation Mode** (default)  
✅ No API key needed  
✅ No external dependencies  
✅ Works immediately  

### For Production:
⚠️ **Use REST API Mode**  
⚠️ Requires Magneti API credentials  
⚠️ Test in sandbox first  
⚠️ Use environment variables for credentials  

---

**Need Help?**
- Check application logs for detailed error messages
- Review the MAGNETI_INTEGRATION.md for detailed API documentation
- Contact Magneti support for API-related issues
