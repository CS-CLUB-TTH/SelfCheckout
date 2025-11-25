package com.selfcheckout.kiosk;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Context;
import android.graphics.Color;
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
 * native printing capabilities via Zebra Link-OS SDK.
 * 
 * JavaScript Interface:
 * - window.Android.printReceipt(zplData) - Print ZPL data to connected printer
 * - window.Android.isPrinterAvailable() - Check if printer is connected
 * - window.Android.getPrinterStatus() - Get printer status as JSON
 */
public class MainActivity extends Activity {
    
    private static final String TAG = "SelfCheckoutKiosk";
    
    // ============================================================
    // CONFIGURATION
    // Change WEB_APP_URL to your ASP.NET Core web app URL
    // Format: https://IP_ADDRESS:PORT/ or http://IP_ADDRESS:PORT/
    // ============================================================
    private static final String WEB_APP_URL = "https://kyrie-deflected-laila.ngrok-free.dev";
    // ============================================================
    
    private WebView webView;
    private PrinterManager printerManager;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // Enable fullscreen kiosk mode
        setupKioskMode();
        
        // Initialize printer manager
        printerManager = new PrinterManager(this);
        
        // Create and configure WebView
        webView = new WebView(this);
        setContentView(webView);
        
        configureWebView();
        
        // Add JavaScript interface for printing
        webView.addJavascriptInterface(new PrintInterface(), "Android");
        
        // Load the web application
        Log.i(TAG, "Loading web app: " + WEB_APP_URL);
        webView.loadUrl(WEB_APP_URL);
    }
    
    /**
     * Configure fullscreen kiosk mode
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
     * JavaScript interface for native printing
     * Exposes methods to the web application via window.Android
     */
    public class PrintInterface {
        
        /**
         * Print ZPL data to the connected Zebra printer
         * Called from JavaScript: window.Android.printReceipt(zplData)
         * 
         * @param zplData ZPL commands to send to the printer
         */
        @JavascriptInterface
        public void printReceipt(final String zplData) {
            Log.i(TAG, "printReceipt called from JavaScript");
            
            if (zplData == null || zplData.isEmpty()) {
                Log.w(TAG, "Empty ZPL data received");
                showToast("No print data received");
                return;
            }
            
            // Print on background thread
            new Thread(() -> {
                try {
                    boolean success = printerManager.printZpl(zplData);
                    
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
         * @return JSON string with printer status
         */
        @JavascriptInterface
        public String getPrinterStatus() {
            String status = printerManager.getPrinterStatusJson();
            Log.i(TAG, "getPrinterStatus: " + status);
            return status;
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
            setupKioskMode();
        }
    }
    
    @Override
    protected void onResume() {
        super.onResume();
        // Reconnect printer if needed
        printerManager.initialize();
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
