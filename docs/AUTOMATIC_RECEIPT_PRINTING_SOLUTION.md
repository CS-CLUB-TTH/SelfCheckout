# Automatic Receipt Printing Solution for Zebra KC50 Kiosk

## Executive Summary

This document describes the **recommended solution** for automatic receipt printing on the Zebra KC50 kiosk. After comprehensive research, the only reliable solution for silent automatic printing without third-party apps is to create a **custom Android WebView wrapper application**.

## The Problem

Your ASP.NET Core web application runs in a browser on the KC50. Android browsers **cannot** silently print to USB/Bluetooth thermal printers - this is a fundamental security restriction of the platform, not a bug or configuration issue.

**What doesn't work:**
- Enterprise Browser - NFC issues, slow performance
- Browser Print API - Not available/working
- Browser print dialog - Requires manual selection (unacceptable for kiosk)
- Third-party apps (RawBT, etc.) - Not recommended for production kiosks

---

## Recommended Solution: Custom Android WebView Wrapper App

### Overview

Create a simple Android application that:
1. **Wraps your web app** in an Android WebView
2. **Exposes a JavaScript interface** for printing
3. **Uses Zebra Link-OS SDK** to communicate directly with the printer via USB
4. **Locks to kiosk mode** for production use

This is the **official Zebra-recommended approach** and uses only Zebra's native SDK.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Android WebView App                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              ASP.NET Core Web App                     │   │
│  │              (loaded in WebView)                      │   │
│  │                                                       │   │
│  │    JavaScript: window.Android.printReceipt(data)     │   │
│  └───────────────────────┬───────────────────────────────┘   │
│                          │                                   │
│                          ▼                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         JavaScript Interface Bridge                  │   │
│  │         @JavascriptInterface                         │   │
│  │         printReceipt(String data)                    │   │
│  └───────────────────────┬───────────────────────────────┘   │
│                          │                                   │
│                          ▼                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         Zebra Link-OS SDK                            │   │
│  │         Direct USB/Bluetooth Communication           │   │
│  └───────────────────────┬───────────────────────────────┘   │
└──────────────────────────┼──────────────────────────────────┘
                           │
                           ▼
               ┌─────────────────────┐
               │   Thermal Printer   │
               │   (USB Connected)   │
               └─────────────────────┘
```

### Implementation Steps

#### Step 1: Create Android Project

Create a new Android project in Android Studio with:
- Minimum SDK: API 26 (Android 8.0) - KC50 runs Android 11
- Target SDK: API 33+

#### Step 2: Add Zebra Link-OS SDK

Add to your `build.gradle`:
```gradle
dependencies {
    implementation 'com.zebra.sdk:linkos-android:2.+'
}
```

Or download from: https://www.zebra.com/us/en/support-downloads/software/printer-software/link-os-multiplatform-sdk.html

#### Step 3: Add Permissions

In `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.USB_HOST" />
<uses-feature android:name="android.hardware.usb.host" />
```

#### Step 4: Create Main Activity

```java
package com.yourcompany.selfcheckout;

import android.app.Activity;
import android.os.Bundle;
import android.webkit.JavascriptInterface;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Toast;

import com.zebra.sdk.comm.Connection;
import com.zebra.sdk.comm.ConnectionException;
import com.zebra.sdk.printer.discovery.UsbDiscoverer;
import com.zebra.sdk.printer.discovery.DiscoveredPrinterUsb;

public class MainActivity extends Activity {
    private WebView webView;
    private Connection printerConnection;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        webView = new WebView(this);
        setContentView(webView);
        
        // Configure WebView
        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(true);
        
        // Add JavaScript interface for printing
        webView.addJavascriptInterface(new PrintInterface(), "Android");
        
        webView.setWebViewClient(new WebViewClient());
        
        // Load your ASP.NET Core web app
        webView.loadUrl("https://your-server-ip:port/");
    }

    /**
     * JavaScript interface for printing
     * Called from web app via: window.Android.printReceipt(data)
     */
    public class PrintInterface {
        
        @JavascriptInterface
        public void printReceipt(String zplData) {
            new Thread(() -> {
                try {
                    // Find USB printer
                    DiscoveredPrinterUsb[] printers = 
                        UsbDiscoverer.getZebraUsbPrinters(getApplicationContext());
                    
                    if (printers.length > 0) {
                        printerConnection = printers[0].getConnection();
                        printerConnection.open();
                        printerConnection.write(zplData.getBytes());
                        printerConnection.close();
                        
                        runOnUiThread(() -> 
                            Toast.makeText(MainActivity.this, 
                                "Receipt printed", Toast.LENGTH_SHORT).show());
                    } else {
                        runOnUiThread(() -> 
                            Toast.makeText(MainActivity.this, 
                                "No printer found", Toast.LENGTH_SHORT).show());
                    }
                } catch (ConnectionException e) {
                    e.printStackTrace();
                    runOnUiThread(() -> 
                        Toast.makeText(MainActivity.this, 
                            "Print error: " + e.getMessage(), Toast.LENGTH_SHORT).show());
                }
            }).start();
        }
        
        @JavascriptInterface
        public boolean isPrinterAvailable() {
            try {
                DiscoveredPrinterUsb[] printers = 
                    UsbDiscoverer.getZebraUsbPrinters(getApplicationContext());
                return printers.length > 0;
            } catch (Exception e) {
                return false;
            }
        }
    }

    @Override
    public void onBackPressed() {
        if (webView.canGoBack()) {
            webView.goBack();
        }
        // Don't call super - prevents exiting kiosk mode
    }
}
```

#### Step 5: Update Web App JavaScript

In your `zebra-printer.js`, the printing will use:

```javascript
async function printReceipt(zplCommands, htmlReceipt) {
    // Check for Android native bridge (custom WebView app)
    if (typeof Android !== 'undefined' && typeof Android.printReceipt === 'function') {
        console.log('Using Android native bridge');
        Android.printReceipt(zplCommands);
        return { success: true, method: 'android-bridge' };
    }
    
    // Fallback for testing in desktop browser
    console.warn('Android bridge not available - running in browser mode');
    // ... fallback code
}
```

#### Step 6: Build and Deploy

1. Build the APK in Android Studio
2. Install on KC50 via USB or MDM
3. Set as default launcher (kiosk mode) using Zebra StageNow or device settings
4. Test the complete flow

---

## Web App Changes Required

### Update Success.cshtml.cs

The current implementation already generates ZPL receipts. The Success page passes this to JavaScript which will call `Android.printReceipt()`.

### Update zebra-printer.js

The JavaScript should prioritize the Android bridge:

```javascript
async printReceipt(zplCommands, htmlReceipt) {
    // Method 1: Android native bridge (PREFERRED for KC50)
    if (typeof Android !== 'undefined' && typeof Android.printReceipt === 'function') {
        console.log('Using Android native bridge');
        try {
            Android.printReceipt(zplCommands);
            return { success: true, method: 'android-bridge' };
        } catch (error) {
            console.error('Android bridge print failed:', error);
        }
    }
    
    // Fallback methods for desktop testing...
}
```

---

## Resources

### Zebra Official Resources
- **Link-OS SDK Download:** https://www.zebra.com/us/en/support-downloads/software/printer-software/link-os-multiplatform-sdk.html
- **Link-OS SDK Documentation:** https://techdocs.zebra.com/link-os/latest/android/
- **KC50 Support Page:** https://www.zebra.com/us/en/support-downloads/interactive-kiosks/kc50.html
- **Sample Code:** https://github.com/ZebraDevs/Zebra-Printer-Samples

### Android Development
- **WebView Documentation:** https://developer.android.com/develop/ui/views/layout/webapps/webview
- **JavaScript Interface:** https://developer.android.com/reference/android/webkit/JavascriptInterface

---

## Benefits of This Approach

✅ **No third-party apps required** - Uses only Zebra's official SDK  
✅ **Silent printing** - No popups or dialogs  
✅ **Direct USB communication** - Fast and reliable  
✅ **Full control** - You own and maintain the code  
✅ **Kiosk-ready** - Can be locked as default launcher  
✅ **Official support** - Zebra-recommended approach  

---

## Development Effort Estimate

| Task | Estimated Time |
|------|---------------|
| Set up Android project | 1-2 hours |
| Integrate Link-OS SDK | 2-3 hours |
| Create WebView wrapper | 1-2 hours |
| Implement print interface | 2-3 hours |
| Testing and debugging | 4-8 hours |
| Kiosk mode configuration | 1-2 hours |
| **Total** | **11-20 hours** |

---

## Alternative: Hire Development

If you don't have Android development resources, you can:
1. Hire a contractor to build this WebView wrapper app
2. Contact Zebra's professional services
3. Use Zebra's partner network for implementation support

---

## Document Information

**Created:** 2025-11-25  
**Last Updated:** 2025-11-25  
**Author:** Development Team  
**Status:** Final Recommendation
