package com.selfcheckout.kiosk;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.ActivityManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.Color;
import android.nfc.NfcAdapter;
import android.nfc.Tag;
import android.nfc.tech.MifareClassic;
import android.nfc.tech.MifareUltralight;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.view.WindowManager;
import android.webkit.ConsoleMessage;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Toast;

/**
 * Main Activity for Self Checkout Kiosk
 * 
 * Wraps the ASP.NET Core web application in a WebView and provides
 * native printing capabilities for EPSON and Zebra printers via USB.
 * 
 * KIOSK LOCKDOWN:
 * This app uses Android's Lock Task Mode (screen pinning) to prevent users
 * from exiting the app. For full lockdown without user confirmation, the app
 * must be set as Device Owner via ADB or MDM.
 * 
 * JavaScript Interface:
 * - window.Android.printReceipt(data) - Print receipt to connected printer
 * - window.Android.isPrinterAvailable() - Check if printer is connected
 * - window.Android.getPrinterStatus() - Get printer status as JSON
 * - window.Android.getPrinterType() - Get printer type (EPSON/ZEBRA/UNKNOWN)
 * - window.Android.isNfcAvailable() - Check if NFC is available
 * - window.Android.isNfcEnabled() - Check if NFC is enabled
 * - window.Android.startNfcScan() - Start NFC foreground dispatch
 * - window.Android.stopNfcScan() - Stop NFC foreground dispatch
 */
public class MainActivity extends Activity {
    
    private static final String TAG = "SelfCheckoutKiosk";
    
    // ============================================================
    // CONFIGURATION
    // Change WEB_APP_URL to your ASP.NET Core web app URL
    // Format: https://IP_ADDRESS:PORT/ or http://IP_ADDRESS:PORT/
    // ============================================================
    private static final String WEB_APP_URL = "https://kyrie-deflected-laila.ngrok-free.dev";
    
    // Enable/disable kiosk lock (screen pinning)
    // Set to true to prevent users from exiting the app
    private static final boolean ENABLE_KIOSK_LOCK = true;
    // ============================================================
    
    private WebView webView;
    private PrinterManager printerManager;
    
    // NFC components
    private NfcAdapter nfcAdapter;
    private PendingIntent nfcPendingIntent;
    private IntentFilter[] nfcIntentFilters;
    private String[][] nfcTechLists;
    private boolean nfcScanActive = false;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // Enable fullscreen kiosk mode
        setupKioskMode();
        
        // Initialize printer manager
        printerManager = new PrinterManager(this);
        
        // Initialize NFC adapter
        initializeNfc();
        
        // Create and configure WebView
        webView = new WebView(this);
        setContentView(webView);
        
        configureWebView();
        
        // Add JavaScript interface for printing and NFC
        webView.addJavascriptInterface(new PrintInterface(), "Android");
        
        // Load the web application
        Log.i(TAG, "Loading web app: " + WEB_APP_URL);
        webView.loadUrl(WEB_APP_URL);
    }
    
    /**
     * Initialize NFC adapter and foreground dispatch
     */
    private void initializeNfc() {
        nfcAdapter = NfcAdapter.getDefaultAdapter(this);
        
        if (nfcAdapter == null) {
            Log.w(TAG, "NFC is not available on this device");
            return;
        }
        
        if (!nfcAdapter.isEnabled()) {
            Log.w(TAG, "NFC is disabled. Please enable NFC in settings.");
        }
        
        // Create PendingIntent for foreground dispatch
        Intent intent = new Intent(this, getClass());
        intent.addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP);
        nfcPendingIntent = PendingIntent.getActivity(
            this, 0, intent, PendingIntent.FLAG_IMMUTABLE
        );
        
        // Setup intent filters for NFC discovery
        IntentFilter ndefFilter = new IntentFilter(NfcAdapter.ACTION_NDEF_DISCOVERED);
        IntentFilter tagFilter = new IntentFilter(NfcAdapter.ACTION_TAG_DISCOVERED);
        IntentFilter techFilter = new IntentFilter(NfcAdapter.ACTION_TECH_DISCOVERED);
        
        nfcIntentFilters = new IntentFilter[] { ndefFilter, tagFilter, techFilter };
        
        // Setup tech lists for NFC discovery
        nfcTechLists = new String[][] {
            new String[] { MifareClassic.class.getName() },
            new String[] { MifareUltralight.class.getName() },
            new String[] { android.nfc.tech.NfcA.class.getName() },
            new String[] { android.nfc.tech.NfcB.class.getName() },
            new String[] { android.nfc.tech.IsoDep.class.getName() },
            new String[] { android.nfc.tech.Ndef.class.getName() }
        };
        
        Log.i(TAG, "NFC initialized successfully");
    }
    
    /**
     * Enable NFC foreground dispatch
     */
    private void enableNfcForegroundDispatch() {
        if (nfcAdapter != null && nfcAdapter.isEnabled()) {
            try {
                nfcAdapter.enableForegroundDispatch(this, nfcPendingIntent, nfcIntentFilters, nfcTechLists);
                nfcScanActive = true;
                Log.i(TAG, "NFC foreground dispatch enabled");
            } catch (Exception e) {
                Log.e(TAG, "Error enabling NFC foreground dispatch: " + e.getMessage());
            }
        }
    }
    
    /**
     * Disable NFC foreground dispatch
     */
    private void disableNfcForegroundDispatch() {
        if (nfcAdapter != null) {
            try {
                nfcAdapter.disableForegroundDispatch(this);
                nfcScanActive = false;
                Log.i(TAG, "NFC foreground dispatch disabled");
            } catch (Exception e) {
                Log.e(TAG, "Error disabling NFC foreground dispatch: " + e.getMessage());
            }
        }
    }
    
    /**
     * Handle NFC intent
     */
    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        
        String action = intent.getAction();
        if (NfcAdapter.ACTION_TAG_DISCOVERED.equals(action) ||
            NfcAdapter.ACTION_TECH_DISCOVERED.equals(action) ||
            NfcAdapter.ACTION_NDEF_DISCOVERED.equals(action)) {
            
            Tag tag = intent.getParcelableExtra(NfcAdapter.EXTRA_TAG);
            if (tag != null) {
                handleNfcTag(tag);
            }
        }
    }
    
    /**
     * Process NFC tag and send to JavaScript
     */
    private void handleNfcTag(Tag tag) {
        byte[] tagId = tag.getId();
        String serialNumber = bytesToHexColonSeparated(tagId);
        
        Log.i(TAG, "NFC Tag detected - Serial: " + serialNumber);
        
        // Sanitize serial number to prevent XSS - only allow hex characters and colons
        String sanitizedSerial = serialNumber.replaceAll("[^0-9A-Fa-f:]", "");
        
        // Notify JavaScript about the NFC tag
        runOnUiThread(() -> {
            String jsCallback = String.format(
                "javascript:if(window.onNfcTagDetected){window.onNfcTagDetected('%s');}",
                sanitizedSerial
            );
            webView.evaluateJavascript(jsCallback, null);
        });
    }
    
    /**
     * Convert byte array to hex string with colon separator
     */
    private String bytesToHexColonSeparated(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.length; i++) {
            if (i > 0) sb.append(":");
            sb.append(String.format("%02X", bytes[i]));
        }
        return sb.toString();
    }
    
    /**
     * Configure fullscreen kiosk mode and enable screen pinning (Lock Task Mode)
     * 
     * Lock Task Mode prevents users from:
     * - Pressing Home button
     * - Using Recent Apps
     * - Accessing notifications
     * - Exiting the app without admin permission
     * 
     * NOTE: For full lockdown without user confirmation dialog, you need to:
     * 1. Set this app as Device Owner via ADB:
     *    adb shell dpm set-device-owner com.selfcheckout.kiosk/.DeviceAdminReceiver
     * 2. Or use Zebra's Enterprise Home Screen (EHS) or an MDM solution
     */
    private void setupKioskMode() {
        // Keep screen on
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        
        // Hide system UI for immersive mode
        View decorView = getWindow().getDecorView();
        decorView.setSystemUiVisibility(
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
            | View.SYSTEM_UI_FLAG_LAYOUT_STABLE
            | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
            | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
            | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
            | View.SYSTEM_UI_FLAG_FULLSCREEN
        );
        
        // Enable Lock Task Mode (screen pinning) if configured
        if (ENABLE_KIOSK_LOCK) {
            startKioskLockTask();
        }
    }
    
    /**
     * Start Lock Task Mode (screen pinning)
     * This locks the user into this app until stopLockTask() is called
     */
    private void startKioskLockTask() {
        try {
            ActivityManager activityManager = (ActivityManager) getSystemService(Context.ACTIVITY_SERVICE);
            
            // Check if we're already in lock task mode
            if (activityManager.getLockTaskModeState() == ActivityManager.LOCK_TASK_MODE_NONE) {
                // Start lock task mode (screen pinning)
                // If app is not Device Owner, this will show a user confirmation dialog
                startLockTask();
                Log.i(TAG, "Lock Task Mode (screen pinning) started");
            } else {
                Log.i(TAG, "Already in Lock Task Mode");
            }
        } catch (Exception e) {
            Log.e(TAG, "Error starting Lock Task Mode: " + e.getMessage());
            // This is not fatal - the app will still work, just not locked
        }
    }
    
    /**
     * Stop Lock Task Mode (for admin use only)
     * Can be called via ADB or through a hidden admin interface
     */
    private void stopKioskLockTask() {
        try {
            stopLockTask();
            Log.i(TAG, "Lock Task Mode stopped");
        } catch (Exception e) {
            Log.e(TAG, "Error stopping Lock Task Mode: " + e.getMessage());
        }
    }
    
    /**
     * Configure WebView settings
     */
    @SuppressLint("SetJavaScriptEnabled")
    private void configureWebView() {
        WebSettings settings = webView.getSettings();
        
        // Enable JavaScript (required for the web app)
        settings.setJavaScriptEnabled(true);
        
        // Enable DOM storage for web app
        settings.setDomStorageEnabled(true);
        
        // Enable local storage
        settings.setDatabaseEnabled(true);
        
        // Allow file access
        settings.setAllowFileAccess(true);
        settings.setAllowContentAccess(true);
        
        // Enable zoom controls (optional, can disable for kiosk)
        settings.setBuiltInZoomControls(false);
        settings.setDisplayZoomControls(false);
        
        // Set cache mode
        settings.setCacheMode(WebSettings.LOAD_DEFAULT);
        
        // Enable mixed content (if needed for local resources)
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_COMPATIBILITY_MODE);
        
        // Set user agent to identify as kiosk app
        String userAgent = settings.getUserAgentString();
        settings.setUserAgentString(userAgent + " SelfCheckoutKiosk/1.0");
        
        // Set background color
        webView.setBackgroundColor(Color.WHITE);
        
        // Configure WebView client
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                // Keep all navigation within the WebView
                return false;
            }
            
            @Override
            public void onPageFinished(WebView view, String url) {
                super.onPageFinished(view, url);
                Log.i(TAG, "Page loaded: " + url);
            }
            
            @Override
            public void onReceivedError(WebView view, int errorCode, String description, String failingUrl) {
                Log.e(TAG, "WebView error: " + description + " for " + failingUrl);
            }
        });
        
        // Configure Chrome client for console logging
        webView.setWebChromeClient(new WebChromeClient() {
            @Override
            public boolean onConsoleMessage(ConsoleMessage consoleMessage) {
                Log.d(TAG, "JS Console: " + consoleMessage.message() 
                    + " -- From line " + consoleMessage.lineNumber() 
                    + " of " + consoleMessage.sourceId());
                return true;
            }
        });
    }
    
    /**
     * JavaScript interface for native printing and NFC
     * Exposes methods to the web application via window.Android
     */
    public class PrintInterface {
        
        /**
         * Print receipt data to the connected printer (EPSON or Zebra)
         * Called from JavaScript: window.Android.printReceipt(printData)
         * 
         * @param printData ESC/POS commands for EPSON or ZPL commands for Zebra
         */
        @JavascriptInterface
        public void printReceipt(final String printData) {
            Log.i(TAG, "printReceipt called from JavaScript");
            
            if (printData == null || printData.isEmpty()) {
                Log.w(TAG, "Empty print data received");
                showToast("No print data received");
                return;
            }
            
            // Print on background thread
            new Thread(() -> {
                try {
                    boolean success = printerManager.printReceipt(printData);
                    
                    if (success) {
                        Log.i(TAG, "Print job sent successfully");
                        showToast("Receipt printed");
                    } else {
                        Log.e(TAG, "Print job failed");
                        showToast("Print failed - check printer");
                    }
                } catch (Exception e) {
                    Log.e(TAG, "Print error: " + e.getMessage(), e);
                    showToast("Print error: " + e.getMessage());
                }
            }).start();
        }
        
        /**
         * Check if a printer is available
         * Called from JavaScript: window.Android.isPrinterAvailable()
         * 
         * @return true if printer is connected
         */
        @JavascriptInterface
        public boolean isPrinterAvailable() {
            boolean available = printerManager.isPrinterAvailable();
            Log.i(TAG, "isPrinterAvailable: " + available);
            return available;
        }
        
        /**
         * Get printer status as JSON
         * Called from JavaScript: window.Android.getPrinterStatus()
         * 
         * @return JSON string with printer status including printer type
         */
        @JavascriptInterface
        public String getPrinterStatus() {
            String status = printerManager.getPrinterStatusJson();
            Log.i(TAG, "getPrinterStatus: " + status);
            return status;
        }
        
        /**
         * Get the type of connected printer
         * Called from JavaScript: window.Android.getPrinterType()
         * 
         * @return "EPSON", "ZEBRA", or "UNKNOWN"
         */
        @JavascriptInterface
        public String getPrinterType() {
            String type = printerManager.getPrinterType().toString();
            Log.i(TAG, "getPrinterType: " + type);
            return type;
        }
        
        /**
         * Check if NFC is available on device
         * Called from JavaScript: window.Android.isNfcAvailable()
         * 
         * @return true if NFC hardware is available
         */
        @JavascriptInterface
        public boolean isNfcAvailable() {
            boolean available = nfcAdapter != null;
            Log.i(TAG, "isNfcAvailable: " + available);
            return available;
        }
        
        /**
         * Check if NFC is enabled
         * Called from JavaScript: window.Android.isNfcEnabled()
         * 
         * @return true if NFC is enabled
         */
        @JavascriptInterface
        public boolean isNfcEnabled() {
            boolean enabled = nfcAdapter != null && nfcAdapter.isEnabled();
            Log.i(TAG, "isNfcEnabled: " + enabled);
            return enabled;
        }
        
        /**
         * Start NFC scanning
         * Called from JavaScript: window.Android.startNfcScan()
         * 
         * @return true if NFC scan started successfully
         */
        @JavascriptInterface
        public boolean startNfcScan() {
            Log.i(TAG, "startNfcScan called from JavaScript");
            
            if (nfcAdapter == null) {
                Log.w(TAG, "NFC not available");
                return false;
            }
            
            if (!nfcAdapter.isEnabled()) {
                Log.w(TAG, "NFC is disabled");
                return false;
            }
            
            runOnUiThread(() -> enableNfcForegroundDispatch());
            return true;
        }
        
        /**
         * Stop NFC scanning
         * Called from JavaScript: window.Android.stopNfcScan()
         */
        @JavascriptInterface
        public void stopNfcScan() {
            Log.i(TAG, "stopNfcScan called from JavaScript");
            runOnUiThread(() -> disableNfcForegroundDispatch());
        }
        
        /**
         * Show toast message on UI thread
         */
        private void showToast(final String message) {
            runOnUiThread(() -> Toast.makeText(MainActivity.this, message, Toast.LENGTH_SHORT).show());
        }
    }
    
    /**
     * Handle back button - prevent exiting kiosk
     */
    @Override
    public void onBackPressed() {
        if (webView.canGoBack()) {
            webView.goBack();
        }
        // Don't call super.onBackPressed() to prevent exiting
    }
    
    /**
     * Restore immersive mode when window focus changes
     */
    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) {
            // Restore immersive mode (hide system bars)
            View decorView = getWindow().getDecorView();
            decorView.setSystemUiVisibility(
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                | View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                | View.SYSTEM_UI_FLAG_FULLSCREEN
            );
        }
    }
    
    @Override
    protected void onResume() {
        super.onResume();
        // Reconnect printer if needed
        printerManager.initialize();
        
        // Always enable NFC foreground dispatch when activity is in foreground
        // This ensures NFC reads work on the first tap
        if (nfcAdapter != null && nfcAdapter.isEnabled()) {
            enableNfcForegroundDispatch();
        }
    }
    
    @Override
    protected void onPause() {
        super.onPause();
        // Disable NFC foreground dispatch to release resources
        disableNfcForegroundDispatch();
    }
    
    @Override
    protected void onDestroy() {
        super.onDestroy();
        printerManager.disconnect();
        if (webView != null) {
            webView.destroy();
        }
    }
}
