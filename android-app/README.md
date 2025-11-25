# Self Checkout Kiosk Android App

This Android application wraps the Self Checkout web application in a WebView and provides native printing capabilities via Zebra Link-OS SDK.

## Features

- **WebView Wrapper**: Loads the ASP.NET Core web application
- **Native Printing**: Direct USB/Bluetooth printing via Zebra Link-OS SDK
- **JavaScript Bridge**: Exposes `window.Android.printReceipt(zplData)` to the web app
- **Kiosk Mode Ready**: Can be configured as default launcher

## Requirements

- Android Studio Arctic Fox (2020.3.1) or later
- Android SDK 26+ (Android 8.0 Oreo)
- Zebra KC50 Kiosk or compatible Android device
- Zebra thermal printer (USB or Bluetooth connected)

## Setup Instructions

### 1. Open in Android Studio

1. Open Android Studio
2. Select "Open an existing project"
3. Navigate to this `android-app` folder
4. Click "OK" and let Gradle sync

### 2. Configure Web App URL

Edit `app/src/main/java/com/selfcheckout/kiosk/MainActivity.java`:

```java
// Change this to your ASP.NET Core app URL
private static final String WEB_APP_URL = "https://your-server-ip:port/";
```

### 3. Add Zebra SDK (if not already included)

The project is configured to use Zebra Link-OS SDK. If you need to update:

1. Download Link-OS SDK from: https://www.zebra.com/us/en/support-downloads/software/printer-software/link-os-multiplatform-sdk.html
2. Copy the `ZSDK_ANDROID_API.jar` to `app/libs/`
3. Sync Gradle

### 4. Build and Deploy

1. Connect the KC50 via USB (enable USB debugging)
2. Click "Run" in Android Studio
3. Select the KC50 device
4. App will install and launch

## Project Structure

```
android-app/
├── app/
│   ├── src/main/
│   │   ├── java/com/selfcheckout/kiosk/
│   │   │   ├── MainActivity.java      # Main activity with WebView
│   │   │   └── PrinterManager.java    # Zebra printer management
│   │   ├── res/
│   │   │   ├── layout/
│   │   │   │   └── activity_main.xml  # Main layout
│   │   │   └── values/
│   │   │       ├── strings.xml        # String resources
│   │   │       ├── colors.xml         # Color resources
│   │   │       └── themes.xml         # App theme
│   │   └── AndroidManifest.xml        # App manifest
│   └── build.gradle                   # App-level build config
├── build.gradle                       # Project-level build config
├── settings.gradle                    # Gradle settings
└── README.md                          # This file
```

## JavaScript Interface

The app exposes these methods to JavaScript:

### `window.Android.printReceipt(zplData)`
Prints ZPL data to the connected Zebra printer.

```javascript
// Example usage in web app
if (typeof Android !== 'undefined' && Android.printReceipt) {
    Android.printReceipt(zplCommands);
}
```

### `window.Android.isPrinterAvailable()`
Returns `true` if a printer is connected.

```javascript
if (Android.isPrinterAvailable()) {
    // Printer is ready
}
```

### `window.Android.getPrinterStatus()`
Returns printer status as a JSON string.

## Kiosk Mode Setup

To run as a kiosk (prevent users from exiting):

### Option 1: Set as Default Launcher
1. Go to Settings > Apps > Default Apps
2. Set "Self Checkout Kiosk" as Home app

### Option 2: Use Zebra StageNow
1. Create a StageNow profile
2. Configure MX App Manager to set this app as default launcher
3. Deploy to device

### Option 3: Device Owner Mode
Use Android's Device Owner mode for enterprise deployment.

## Troubleshooting

### Printer Not Found
- Ensure printer is connected via USB or paired via Bluetooth
- Check USB permissions in Android settings
- Verify printer is powered on

### WebView Not Loading
- Check network connectivity
- Verify the WEB_APP_URL is correct
- Check if HTTPS certificate is valid (or add exception for self-signed)

### Build Errors
- Sync Gradle files
- Check SDK versions match requirements
- Ensure Zebra SDK JAR is in libs folder

## License

This project is proprietary software for Self Checkout Kiosk system.
