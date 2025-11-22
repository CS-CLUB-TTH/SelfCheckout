# Code Architecture & Best Practices

## Project Structure

```
SelfCheckoutKiosk/
├── Models/                          # Data models and DTOs
│   ├── BillDetail.cs               # Database entity
│   ├── MstCustomerSupplier.cs      # Database entity
│   ├── MagnetiPaymentRequest.cs    # Payment request DTO
│   ├── MagnetiPaymentResponse.cs   # Payment response DTO
│   └── MagnetiApiResponse.cs       # API response mapping model
│
├── Services/                        # Business logic layer
│   ├── IDatabaseService.cs         # Database service interface
│   ├── DatabaseService.cs          # Database service implementation
│   ├── IMagnetiPaymentService.cs   # Payment service interface
│   └── MagnetiPaymentService.cs    # Payment service implementation
│
├── Pages/                           # Razor Pages (UI + Page Models)
│   ├── Cart.cshtml                 # Cart view
│   ├── Cart.cshtml.cs              # Cart page model (UI logic)
│   ├── Payment.cshtml              # Payment view
│   ├── Payment.cshtml.cs           # Payment page model
│   └── ...
│
└── wwwroot/                         # Static files (CSS, JS, images)
```

## Architectural Principles

### 1. **Separation of Concerns** ✅

**Models** (Data Layer)
- Define data structures
- No business logic
- Reusable across services
- Easy to test and maintain

**Services** (Business Logic Layer)
- Implement business rules
- Handle external integrations (API, Database)
- Process and transform data
- Independent and testable

**Pages** (Presentation Layer)
- Handle HTTP requests/responses
- Coordinate between services
- Prepare data for views
- User interaction logic

### 2. **Why Models Should Be Separate**

#### ❌ **Bad Practice**: Inner Classes in Services
```csharp
public class MagnetiPaymentService
{
    // Service logic...
    
    private class MagnetiApiResponse  // ❌ Hidden, not reusable
    {
        public string? Status { get; set; }
    }
}
```

**Problems:**
- Not reusable in other services
- Harder to test
- Poor discoverability
- Violates separation of concerns

#### ✅ **Good Practice**: Separate Model Files
```csharp
// Models/MagnetiApiResponse.cs
public class MagnetiApiResponse  // ✅ Reusable, testable
{
    public string? Status { get; set; }
}

// Services/MagnetiPaymentService.cs
public class MagnetiPaymentService
{
    // Service logic uses the model
}
```

**Benefits:**
- Reusable across services
- Easy to unit test
- Clear organization
- Better maintainability

### 3. **Dependency Injection** ✅

All services are registered in `Program.cs`:

```csharp
// Register services
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IMagnetiPaymentService, MagnetiPaymentService>();
```

**Benefits:**
- Loose coupling
- Easy to mock for testing
- Configurable implementations
- Follows SOLID principles

### 4. **Interface-Based Design** ✅

Every service has an interface:

```csharp
public interface IDatabaseService
{
    Task<int?> GetCustomerKeyByCardNo(string cardNo);
    Task<List<BillDetail>> GetBillDetailsByCustomerKey(int cusKey, int workstationId = 0);
}

public class DatabaseService : IDatabaseService
{
    // Implementation
}
```

**Benefits:**
- Contract-first development
- Easy to create mocks for testing
- Supports multiple implementations
- Clear API documentation

### 5. **Async/Await Pattern** ✅

All I/O operations use async:

```csharp
public async Task<MagnetiPaymentResponse> ProcessPaymentAsync(MagnetiPaymentRequest request)
{
    // Async operations
    await Task.Delay(1500);
    var response = await _httpClient.PostAsync(...);
}
```

**Benefits:**
- Non-blocking I/O
- Better scalability
- Improved responsiveness

### 6. **Logging Best Practices** ✅

Structured logging throughout:

```csharp
_logger.LogInformation(
    "Processing payment - TransactionId: {TransactionId}, Amount: {Amount} {Currency}",
    request.TransactionId, request.Amount, request.Currency);

_logger.LogError(ex, "Error processing payment for transaction {TransactionId}", request.TransactionId);
```

**Benefits:**
- Structured data for analysis
- Easy debugging
- Performance monitoring

### 7. **Configuration Management** ✅

Settings in `appsettings.json`:

```csharp
// Read configuration
_useSimulation = _configuration.GetValue<bool>("Magneti:UseSimulation", true);
var apiKey = _configuration[ApiKeyKey];
```

**Benefits:**
- Environment-specific settings
- No hardcoded values
- Easy to change without recompiling

## File Organization Rules

### ✅ **DO:**

1. **Keep models in `/Models` folder**
   - All DTOs, entities, request/response models
   - Public classes that represent data

2. **Keep services in `/Services` folder**
   - Business logic
   - External integrations
   - Data processing

3. **Keep page models in `/Pages` folder**
   - UI-specific logic
   - Request handling
   - View preparation

4. **One class per file**
   - File name matches class name
   - Easier to find and navigate

5. **Use descriptive names**
   - `MagnetiPaymentService` (not `PaymentHandler`)
   - `BillDetail` (not `Data1`)

### ❌ **DON'T:**

1. **Don't put models inside services**
   - Makes them hard to reuse
   - Poor separation of concerns

2. **Don't mix concerns**
   - No database logic in page models
   - No UI logic in services

3. **Don't create "God classes"**
   - Keep classes focused
   - Single responsibility principle

## Layer Communication

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│    (Razor Pages - Cart, Payment, etc.)  │
└────────────┬────────────────────────────┘
             │ Calls
             ↓
┌─────────────────────────────────────────┐
│         Business Logic Layer            │
│  (Services - MagnetiPaymentService,     │
│              DatabaseService)            │
└────────────┬────────────────────────────┘
             │ Uses
             ↓
┌─────────────────────────────────────────┐
│           Data Layer                    │
│  (Models - Request/Response DTOs,       │
│           Entities)                     │
└─────────────────────────────────────────┘
             │
             ↓
┌─────────────────────────────────────────┐
│      External Systems                   │
│  (Database, Magneti API)                │
└─────────────────────────────────────────┘
```

## Testing Strategy

With this architecture, you can easily test:

### Unit Tests
```csharp
// Test service with mocked dependencies
var mockConfig = new Mock<IConfiguration>();
var mockLogger = new Mock<ILogger<MagnetiPaymentService>>();
var mockHttpClientFactory = new Mock<IHttpClientFactory>();

var service = new MagnetiPaymentService(
    mockConfig.Object,
    mockLogger.Object,
    mockHttpClientFactory.Object
);
```

### Integration Tests
```csharp
// Test database service with real database
var service = new DatabaseService(configuration, logger);
var customer = await service.GetCustomerKeyByCardNo("CARD123");
```

## Summary

### Current Implementation ✅

| Aspect | Implementation | Status |
|--------|---------------|--------|
| Models in separate files | ✅ Yes | Correct |
| Services in separate files | ✅ Yes | Correct |
| Dependency Injection | ✅ Yes | Correct |
| Interface-based design | ✅ Yes | Correct |
| Async/await pattern | ✅ Yes | Correct |
| Structured logging | ✅ Yes | Correct |
| Configuration management | ✅ Yes | Correct |

### Key Takeaways

1. **Models = Data** → Separate files in `/Models`
2. **Services = Logic** → Separate files in `/Services`
3. **Pages = UI** → Separate files in `/Pages`
4. **Always use interfaces** for services
5. **Dependency injection** for all dependencies
6. **One class per file** for clarity
7. **Async all the way** for I/O operations

This architecture ensures:
- ✅ Easy to maintain
- ✅ Easy to test
- ✅ Easy to extend
- ✅ Clear organization
- ✅ Follows industry standards

---

**References:**
- [ASP.NET Core Architecture Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
