# Self Checkout Kiosk

A modern self-checkout kiosk application built with ASP.NET Core Razor Pages, featuring NFC card integration and automated cart loading.

## Features

- **NFC Card Integration**: Customers can tap their NFC card to automatically load their cart
- **Dynamic Cart Loading**: Cart items are loaded from the database based on customer's pending orders
- **Real-time Total Calculation**: Automatically calculates subtotal, tax (VAT), and total
- **Modern UI**: Clean, responsive interface optimized for kiosk displays
- **PWA Support**: Progressive Web App capabilities for offline support

## Prerequisites

- .NET 8.0 SDK
- SQL Server database with the CUBES schema
- NFC-compatible device/browser (for testing NFC functionality)

## Database Setup

The application requires a SQL Server database with the following:

1. **Database**: CUBES
2. **Required Tables**:
   - `mst_customer_supplier`: Contains customer information including card numbers
     - `cus_key` (int): Customer primary key
     - `card_no` (varchar): NFC card number
   - Related tables for products, bill headers, and bill details

3. **Required Stored Procedure**: `dbo.retrieve_bill_details2`
   - Parameters: `@cus_key` (int), `@ws` (int - workstation ID)
   - Returns bill details for unpaid orders of a customer

## Configuration

1. **Update Database Connection String**

   Edit `appsettings.json` and update the connection string:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=CUBES;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
     }
   }
   ```

   **Security Note**: For production, use environment variables or Azure Key Vault instead of storing credentials in appsettings.json:

   ```bash
   # Linux/macOS
   export ConnectionStrings__DefaultConnection="Server=..."
   
   # Windows
   set ConnectionStrings__DefaultConnection=Server=...
   ```

2. **Configure for Development**

   Create `appsettings.Development.json` (this file is gitignored):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Your-Development-Connection-String-Here"
     }
   }
   ```

## Running the Application

1. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

2. **Build the Project**:
   ```bash
   dotnet build
   ```

3. **Run the Application**:
   ```bash
   dotnet run
   ```

4. **Access the Application**:
   - Open browser and navigate to `https://localhost:5001` (or the URL shown in console)

## How It Works

### NFC Flow

1. Customer navigates to the NFC page (`/Nfc`)
2. Customer taps their NFC card on the reader
3. The application:
   - Reads the card's serial number or payload
   - Sends the card number to the backend (`/Cart/LoadFromCard?cardNo=...`)
4. Backend processes the request:
   - Looks up `cus_key` from `mst_customer_supplier` table using `card_no`
   - Calls `retrieve_bill_details2` stored procedure with the `cus_key`
   - Returns bill details (products, quantities, prices, taxes)
5. Cart page displays:
   - All products with descriptions, quantities, and amounts
   - Subtotal (sum of all item amounts)
   - Tax (sum of all VAT amounts)
   - Total (subtotal + tax)

### Cart Display

The cart dynamically displays:
- **Product Description**: Item name (may include barcode or menu group based on configuration)
- **Quantity**: Number of items ordered
- **Amount**: Line total for each item
- **Subtotal**: Sum of all item amounts before tax
- **Tax (VAT)**: Sum of all VAT amounts
- **Total**: Final amount to pay (subtotal + tax)

### Error Handling

The application handles various error cases:
- **Invalid Card**: Shows error message if no customer found
- **Empty Cart**: Informs user if no pending items exist
- **Database Errors**: Logs errors and displays user-friendly message
- **NFC Not Supported**: Detects and informs if device doesn't support NFC

## Project Structure

```
SelfCheckoutKiosk/
├── Models/
│   ├── BillDetail.cs           # Bill detail entity model
│   └── MstCustomerSupplier.cs  # Customer model
├── Services/
│   └── DatabaseService.cs      # Database access layer
├── Pages/
│   ├── Nfc.cshtml             # NFC card scanning page
│   ├── Nfc.cshtml.cs
│   ├── Cart.cshtml            # Shopping cart page
│   ├── Cart.cshtml.cs         # Cart backend logic
│   └── ...
├── wwwroot/
│   ├── js/
│   │   └── kiosk.js           # NFC and cart JavaScript
│   └── css/
│       └── kiosk.css          # Kiosk styling
├── appsettings.json           # Configuration (template)
└── Program.cs                 # Application entry point
```

## API Endpoints

### GET /Cart/LoadFromCard

Loads cart items for a customer based on their NFC card number.

**Parameters**:
- `cardNo` (query string): NFC card number

**Returns**:
- Renders Cart page with loaded items
- Shows error message if card not found or no items exist

**Example**:
```
GET /Cart/LoadFromCard?cardNo=ABC123XYZ
```

## Technologies Used

- **Backend**: ASP.NET Core 8.0 (Razor Pages)
- **Database**: Microsoft SQL Server
- **ORM**: Dapper (micro-ORM)
- **Frontend**: HTML5, CSS3, JavaScript
- **NFC**: Web NFC API (NDEFReader)
- **Styling**: Bootstrap 5 + Custom CSS

## Browser Requirements

For NFC functionality:
- Chrome 89+ (Android)
- Chrome 100+ (Desktop with experimental flags)
- Requires HTTPS connection
- NFC permission must be granted

## Development Notes

### Database Service

The `DatabaseService` provides two main methods:

1. **GetCustomerKeyByCardNo(string cardNo)**
   - Queries: `SELECT TOP 1 cus_key FROM mst_customer_supplier WHERE card_no = @CardNo`
   - Returns customer key or null if not found

2. **GetBillDetailsByCustomerKey(int cusKey, int workstationId = 0)**
   - Calls: `dbo.retrieve_bill_details2` stored procedure
   - Returns list of bill details for unpaid orders

### Logging

The application uses structured logging (ILogger) to track:
- NFC card reads
- Customer lookups
- Cart loading operations
- Errors and exceptions

Check application logs for troubleshooting.

## Security Considerations

1. **Connection Strings**: Never commit real database credentials to source control
2. **SQL Injection**: All queries use parameterized commands (protected)
3. **NFC Data**: Card data is sanitized and validated before use
4. **HTTPS**: Required for NFC API to function
5. **Input Validation**: All user inputs are validated server-side

## Troubleshooting

### NFC Not Working

1. Ensure HTTPS is enabled
2. Check browser compatibility
3. Grant NFC permissions in browser
4. Verify NFC is enabled on device

### Database Connection Issues

1. Verify connection string in appsettings.json
2. Check SQL Server is running and accessible
3. Verify firewall rules allow connection
4. Check database credentials and permissions

### No Items Loading

1. Verify customer exists in `mst_customer_supplier` with correct `card_no`
2. Check stored procedure `retrieve_bill_details2` exists and works
3. Verify customer has unpaid orders in the system
4. Check application logs for specific errors

## License

[Your License Here]

## Support

For issues or questions, please contact the development team.
