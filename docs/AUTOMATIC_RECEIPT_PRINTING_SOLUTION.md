# Automatic Receipt Printing - Android WebView App

## Overview

This solution provides automatic receipt printing on the Zebra KC50 kiosk using a custom Android WebView wrapper application. The Android app is **included in this repository** and ready to build.

**Location:** `android-app/`

## Quick Start

### 1. Open in Android Studio

1. Open Android Studio
2. Select "Open an existing project"
3. Navigate to `android-app/` folder
4. Click "OK" and wait for Gradle sync

### 2. Configure Web App URL

Edit `android-app/app/src/main/java/com/selfcheckout/kiosk/MainActivity.java`:

```java
// Line 35 - Change this to your ASP.NET Core app URL
private static final String WEB_APP_URL = "https://192.168.1.100:5001/";
```

### 3. Add Zebra SDK (Optional but Recommended)

1. Download from: https://www.zebra.com/us/en/support-downloads/software/printer-software/link-os-multiplatform-sdk.html
2. Extract and copy `ZSDK_ANDROID_API.jar` to `android-app/app/libs/`
3. Rebuild project

**Note:** The app works without the SDK using direct USB communication.

### 4. Build and Deploy

1. Connect KC50 via USB (enable USB debugging in Settings > Developer Options)
2. Click "Run" (green play button) in Android Studio
3. Select the KC50 device
4. App installs and launches automatically

### 5. Set as Default Launcher (Kiosk Mode)

On the KC50:
1. Settings > Apps > Default Apps > Home App
2. Select "Self Checkout Kiosk"

---

## How It Works

### Architecture

```
┌──────────────────────────────────────────────────────────┐
│              Self Checkout Kiosk Android App              │
│                                                          │
│  ┌────────────────────────────────────────────────────┐ │
│  │           WebView (loads your web app)             │ │
│  │                                                    │ │
│  │  Your ASP.NET Core app runs here                   │ │
│  │  JavaScript calls: Android.printReceipt(zplData)  │ │
│  └──────────────────────┬─────────────────────────────┘ │
│                         │                                │
│                         ▼                                │
│  ┌────────────────────────────────────────────────────┐ │
│  │           JavaScript Interface Bridge              │ │
│  │           @JavascriptInterface                     │ │
│  │           printReceipt(String zplData)             │ │
│  └──────────────────────┬─────────────────────────────┘ │
│                         │                                │
│                         ▼                                │
│  ┌────────────────────────────────────────────────────┐ │
│  │              PrinterManager                        │ │
│  │  - Zebra Link-OS SDK (if available)               │ │
│  │  - Direct USB (fallback)                          │ │
│  └──────────────────────┬─────────────────────────────┘ │
└─────────────────────────┼────────────────────────────────┘
                          │
                          ▼
               ┌───────────────────┐
               │   Zebra Printer   │
               │  (USB Connected)  │
               └───────────────────┘
```

### Web App Integration

Your web app's JavaScript calls the Android native methods:

```javascript
// Print a receipt (ZPL format)
if (typeof Android !== 'undefined' && Android.printReceipt) {
    Android.printReceipt(zplData);
}

// Check if printer is available
if (Android.isPrinterAvailable()) {
    console.log('Printer ready');
}

// Get printer status
const status = JSON.parse(Android.getPrinterStatus());
console.log(status);
```

---

## Project Structure

```
android-app/
├── README.md                    # Android app documentation
├── build.gradle                 # Project build config
├── settings.gradle              # Gradle settings
├── app/
│   ├── build.gradle             # App build config
│   ├── proguard-rules.pro       # ProGuard rules
│   ├── libs/                    # Place Zebra SDK JAR here
│   │   └── README.md
│   └── src/main/
│       ├── AndroidManifest.xml  # App permissions & config
│       ├── java/com/selfcheckout/kiosk/
│       │   ├── MainActivity.java     # WebView & JS interface
│       │   └── PrinterManager.java   # Printer communication
│       └── res/
│           ├── layout/
│           │   └── activity_main.xml
│           ├── values/
│           │   ├── strings.xml
│           │   ├── colors.xml
│           │   └── themes.xml
│           └── xml/
│               └── usb_device_filter.xml
└── gradle/wrapper/
    └── gradle-wrapper.properties
```

---

## JavaScript Interface Methods

### `window.Android.printReceipt(zplData)`

Prints ZPL data to the connected Zebra printer.

**Parameters:**
- `zplData` (String): ZPL commands to send to the printer

**Example:**
```javascript
const zpl = '^XA^FO50,50^A0N,30,30^FDHello World^FS^XZ';
Android.printReceipt(zpl);
```

### `window.Android.isPrinterAvailable()`

Checks if a printer is connected.

**Returns:** `boolean` - true if printer is available

**Example:**
```javascript
if (Android.isPrinterAvailable()) {
    Android.printReceipt(zplData);
} else {
    console.error('No printer connected');
}
```

### `window.Android.getPrinterStatus()`

Gets detailed printer status as JSON string.

**Returns:** JSON string with:
- `available`: boolean
- `zebraSdkAvailable`: boolean
- `deviceName`: string (if connected)
- `vendorId`: number (if connected)
- `productId`: number (if connected)

**Example:**
```javascript
const status = JSON.parse(Android.getPrinterStatus());
console.log('Printer available:', status.available);
```

---

## Requirements

- **Android Studio:** Arctic Fox (2020.3.1) or later
- **Android SDK:** API 26+ (Android 8.0 Oreo)
- **Target Device:** Zebra KC50 or compatible Android device
- **Printer:** Zebra thermal printer (USB connected)

---

## Troubleshooting

### Printer Not Found

1. Check USB cable connection
2. Verify printer is powered on
3. Go to Android Settings > Connected Devices and check for printer
4. May need to approve USB permission popup

### WebView Not Loading

1. Check network connectivity between KC50 and web server
2. Verify URL in MainActivity.java is correct
3. For HTTPS with self-signed certificate, you may need to accept it manually first
4. Check Android logcat for errors

### Build Errors

1. File > Sync Project with Gradle Files
2. Build > Clean Project, then Build > Rebuild
3. Verify Android SDK is installed (Tools > SDK Manager)
4. Check that all SDK components are up to date

### Print Job Fails

1. Check logcat for error messages (filter by "PrinterManager")
2. Verify ZPL syntax is correct
3. Try printing a test label from Zebra Printer Setup Utility app
4. Check paper is loaded correctly

---

## Benefits

✅ **No third-party apps** - Uses only Zebra's official SDK  
✅ **Silent printing** - No popups or dialogs  
✅ **Direct USB** - Fast and reliable communication  
✅ **Full control** - You own the code  
✅ **Kiosk ready** - Fullscreen, prevents exit  
✅ **Fallback support** - Works without Zebra SDK  

---

## Resources

- **Zebra Link-OS SDK:** https://www.zebra.com/us/en/support-downloads/software/printer-software/link-os-multiplatform-sdk.html
- **KC50 Support:** https://www.zebra.com/us/en/support-downloads/interactive-kiosks/kc50.html
- **ZPL Programming Guide:** https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf
- **Zebra Samples:** https://github.com/ZebraDevs/Zebra-Printer-Samples

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-25
