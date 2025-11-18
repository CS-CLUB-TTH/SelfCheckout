# Self Checkout Kiosk Application

A modern, web-based self-checkout kiosk application for Farmers Market built with ASP.NET Core.

## Features

- **Idle Screen**: Displays promotional videos with a "Press to Start" button
- **Shopping Cart**: Shows items with quantities, prices, and total calculation
- **Payment Processing**: Integration placeholder for Magnati payment terminal
- **Payment Success**: Confirmation screen after successful payment
- **Feedback System**: 5-face rating icons (red to green) with QR code for detailed feedback

## Branding

The application follows Farmers Market branding guidelines:
- **Primary Colors**: Yellow (#f4d84b), Dark Green (#163f36), Green (#4b9762)
- **Secondary Colors**: Purple, Light Green, Orange, Red
- **Fonts**: Montserrat (primary), clean and modern typography
- **Design**: Modern, clean interface similar to McDonald's/KFC self-order kiosks

## Technology Stack

- ASP.NET Core 8.0 (Razor Pages)
- Bootstrap 5
- Custom CSS for Farmers Market branding
- JavaScript for interactive features

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later

### Running the Application

1. Navigate to the project directory:
   ```bash
   cd SelfCheckoutKiosk
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to:
   ```
   http://localhost:5000
   ```

### Building the Application

```bash
cd SelfCheckoutKiosk
dotnet build
```

## Application Flow

1. **Idle Screen** → Customer taps "Press to Start"
2. **Cart Screen** → Review items and tap "Proceed to Pay"
3. **Payment Screen** → Magnati terminal activates (placeholder for hardware integration)
4. **Success Screen** → Payment success message displayed
5. **Feedback Screen** → 5-face rating system with QR code for detailed feedback
6. **Auto-return** → Returns to Idle screen after 60 seconds of inactivity

## Project Structure

```
SelfCheckoutKiosk/
├── Pages/
│   ├── Index.cshtml           # Idle screen
│   ├── Cart.cshtml            # Shopping cart
│   ├── Payment.cshtml         # Payment processing
│   ├── Success.cshtml         # Payment success
│   ├── Feedback.cshtml        # Feedback system
│   └── Shared/
│       └── _KioskLayout.cshtml # Kiosk-specific layout
├── wwwroot/
│   ├── css/
│   │   └── kiosk.css          # Custom branding styles
│   └── js/
│       └── kiosk.js           # Interactive features
└── Program.cs                 # Application entry point
```

## Features in Detail

### Inactivity Timer
- Automatically returns to idle screen after 60 seconds of inactivity
- Tracks user interactions (clicks, touches, mouse movements)

### Payment Integration
- Placeholder for Magnati terminal integration
- Ready for hardware integration with payment processing logic

### Feedback System
- 5-face rating icons with colors from red (poor) to green (excellent)
- QR code linking to detailed feedback form
- Displayed only after successful payment
- Auto-returns to idle screen after rating selection

## Future Enhancements

- Integration with actual Magnati payment terminal
- Real-time cart data from customer card
- Video player implementation for promotional content
- Backend API for storing feedback ratings
- Multi-language support (English/Arabic)