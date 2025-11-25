package com.selfcheckout.kiosk;

import android.content.Context;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbManager;
import android.util.Log;

import org.json.JSONObject;

import java.util.HashMap;
import java.util.Map;

/**
 * Printer Manager for Zebra Link-OS SDK
 * 
 * Handles printer discovery, connection, and printing via USB.
 * 
 * IMPORTANT: You must add the Zebra Link-OS SDK JAR file to the libs folder:
 * 1. Download from: https://www.zebra.com/us/en/support-downloads/software/printer-software/link-os-multiplatform-sdk.html
 * 2. Copy ZSDK_ANDROID_API.jar to app/libs/
 * 3. Rebuild the project
 * 
 * If the SDK is not available, this class will use direct USB communication
 * as a fallback (basic ZPL printing only).
 */
public class PrinterManager {
    
    private static final String TAG = "PrinterManager";
    
    // Zebra USB Vendor ID
    private static final int ZEBRA_VENDOR_ID = 0x0A5F;
    
    private final Context context;
    private final UsbManager usbManager;
    private UsbDevice connectedPrinter;
    private boolean zebraSdkAvailable = false;
    
    // Zebra SDK objects (loaded dynamically if SDK is available)
    private Object zebraConnection;
    private Object zebraPrinter;
    
    public PrinterManager(Context context) {
        this.context = context;
        this.usbManager = (UsbManager) context.getSystemService(Context.USB_SERVICE);
        
        // Check if Zebra SDK is available
        checkZebraSdkAvailability();
        
        // Initialize connection
        initialize();
    }
    
    /**
     * Check if Zebra Link-OS SDK is available
     */
    private void checkZebraSdkAvailability() {
        try {
            // Try to load Zebra SDK classes
            Class.forName("com.zebra.sdk.comm.UsbConnection");
            Class.forName("com.zebra.sdk.printer.ZebraPrinterFactory");
            zebraSdkAvailable = true;
            Log.i(TAG, "Zebra Link-OS SDK is available");
        } catch (ClassNotFoundException e) {
            zebraSdkAvailable = false;
            Log.w(TAG, "Zebra Link-OS SDK not found - using fallback USB printing");
        }
    }
    
    /**
     * Initialize printer connection
     */
    public void initialize() {
        Log.i(TAG, "Initializing printer manager...");
        findConnectedPrinter();
    }
    
    /**
     * Find connected Zebra USB printer
     */
    private void findConnectedPrinter() {
        if (usbManager == null) {
            Log.e(TAG, "USB Manager not available");
            return;
        }
        
        HashMap<String, UsbDevice> deviceList = usbManager.getDeviceList();
        Log.i(TAG, "Found " + deviceList.size() + " USB devices");
        
        for (Map.Entry<String, UsbDevice> entry : deviceList.entrySet()) {
            UsbDevice device = entry.getValue();
            Log.d(TAG, "USB Device: " + device.getDeviceName() 
                + " VID: " + device.getVendorId() 
                + " PID: " + device.getProductId());
            
            // Check for Zebra printer
            if (device.getVendorId() == ZEBRA_VENDOR_ID) {
                connectedPrinter = device;
                Log.i(TAG, "Found Zebra printer: " + device.getDeviceName());
                break;
            }
        }
        
        if (connectedPrinter == null) {
            Log.w(TAG, "No Zebra printer found");
        }
    }
    
    /**
     * Check if a printer is available
     */
    public boolean isPrinterAvailable() {
        if (connectedPrinter == null) {
            findConnectedPrinter();
        }
        return connectedPrinter != null;
    }
    
    /**
     * Print ZPL data to the connected printer
     * 
     * @param zplData ZPL commands to print
     * @return true if printing succeeded
     */
    public boolean printZpl(String zplData) {
        if (!isPrinterAvailable()) {
            Log.e(TAG, "No printer available");
            return false;
        }
        
        if (zebraSdkAvailable) {
            return printWithZebraSdk(zplData);
        } else {
            return printWithDirectUsb(zplData);
        }
    }
    
    /**
     * Print using Zebra Link-OS SDK
     */
    private boolean printWithZebraSdk(String zplData) {
        try {
            Log.i(TAG, "Printing with Zebra SDK...");
            
            // Use reflection to call Zebra SDK (allows compilation without SDK)
            Class<?> usbConnectionClass = Class.forName("com.zebra.sdk.comm.UsbConnection");
            Class<?> connectionClass = Class.forName("com.zebra.sdk.comm.Connection");
            
            // Create USB connection
            Object connection = usbConnectionClass
                .getConstructor(UsbManager.class, UsbDevice.class)
                .newInstance(usbManager, connectedPrinter);
            
            // Open connection
            connectionClass.getMethod("open").invoke(connection);
            
            // Write ZPL data
            byte[] data = zplData.getBytes("UTF-8");
            connectionClass.getMethod("write", byte[].class).invoke(connection, data);
            
            // Close connection
            connectionClass.getMethod("close").invoke(connection);
            
            Log.i(TAG, "Print job sent successfully via Zebra SDK");
            return true;
            
        } catch (Exception e) {
            Log.e(TAG, "Zebra SDK print error: " + e.getMessage(), e);
            // Try fallback
            return printWithDirectUsb(zplData);
        }
    }
    
    /**
     * Print using direct USB communication (fallback if SDK not available)
     */
    private boolean printWithDirectUsb(String zplData) {
        android.hardware.usb.UsbDeviceConnection connection = null;
        android.hardware.usb.UsbInterface intf = null;
        
        try {
            Log.i(TAG, "Printing with direct USB...");
            
            if (!usbManager.hasPermission(connectedPrinter)) {
                Log.e(TAG, "No USB permission for printer");
                return false;
            }
            
            connection = usbManager.openDevice(connectedPrinter);
            
            if (connection == null) {
                Log.e(TAG, "Failed to open USB connection");
                return false;
            }
            
            // Find the bulk OUT endpoint
            intf = connectedPrinter.getInterface(0);
            android.hardware.usb.UsbEndpoint endpointOut = null;
            
            for (int i = 0; i < intf.getEndpointCount(); i++) {
                android.hardware.usb.UsbEndpoint ep = intf.getEndpoint(i);
                if (ep.getType() == android.hardware.usb.UsbConstants.USB_ENDPOINT_XFER_BULK
                    && ep.getDirection() == android.hardware.usb.UsbConstants.USB_DIR_OUT) {
                    endpointOut = ep;
                    break;
                }
            }
            
            if (endpointOut == null) {
                Log.e(TAG, "No bulk OUT endpoint found");
                return false;
            }
            
            // Claim interface
            connection.claimInterface(intf, true);
            
            // Send ZPL data
            byte[] data = zplData.getBytes("UTF-8");
            int result = connection.bulkTransfer(endpointOut, data, data.length, 5000);
            
            if (result >= 0) {
                Log.i(TAG, "Print job sent successfully via direct USB (" + result + " bytes)");
                return true;
            } else {
                Log.e(TAG, "USB bulk transfer failed: " + result);
                return false;
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Direct USB print error: " + e.getMessage(), e);
            return false;
        } finally {
            // Always release resources
            if (connection != null) {
                if (intf != null) {
                    connection.releaseInterface(intf);
                }
                connection.close();
            }
        }
    }
    
    /**
     * Get printer status as JSON string
     */
    public String getPrinterStatusJson() {
        try {
            JSONObject status = new JSONObject();
            status.put("available", isPrinterAvailable());
            status.put("zebraSdkAvailable", zebraSdkAvailable);
            
            if (connectedPrinter != null) {
                status.put("deviceName", connectedPrinter.getDeviceName());
                status.put("vendorId", connectedPrinter.getVendorId());
                status.put("productId", connectedPrinter.getProductId());
            }
            
            return status.toString();
        } catch (Exception e) {
            return "{\"error\": \"" + e.getMessage() + "\"}";
        }
    }
    
    /**
     * Disconnect from printer
     */
    public void disconnect() {
        Log.i(TAG, "Disconnecting printer...");
        connectedPrinter = null;
        zebraConnection = null;
        zebraPrinter = null;
    }
}
