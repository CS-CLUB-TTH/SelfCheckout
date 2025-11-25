package com.selfcheckout.kiosk;

import android.content.Context;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbEndpoint;
import android.hardware.usb.UsbInterface;
import android.hardware.usb.UsbManager;
import android.hardware.usb.UsbConstants;
import android.util.Log;

import org.json.JSONObject;

import java.util.HashMap;
import java.util.Map;

/**
 * Printer Manager for USB Receipt Printers
 * 
 * Supports both EPSON (ESC/POS) and Zebra (ZPL) thermal receipt printers via USB.
 * 
 * EPSON Printers (TM-T88VII, etc.):
 * - Use ESC/POS commands
 * - Vendor ID: 0x04B8 (1208)
 * 
 * Zebra Printers:
 * - Use ZPL commands
 * - Vendor ID: 0x0A5F (2655)
 */
public class PrinterManager {
    
    private static final String TAG = "PrinterManager";
    
    // USB Vendor IDs
    private static final int ZEBRA_VENDOR_ID = 0x0A5F;  // 2655
    private static final int EPSON_VENDOR_ID = 0x04B8;  // 1208
    
    // Printer types
    public enum PrinterType {
        UNKNOWN,
        ZEBRA,
        EPSON
    }
    
    private final Context context;
    private final UsbManager usbManager;
    private UsbDevice connectedPrinter;
    private PrinterType printerType = PrinterType.UNKNOWN;
    
    public PrinterManager(Context context) {
        this.context = context;
        this.usbManager = (UsbManager) context.getSystemService(Context.USB_SERVICE);
        
        // Initialize connection
        initialize();
    }
    
    /**
     * Initialize printer connection
     */
    public void initialize() {
        Log.i(TAG, "Initializing printer manager...");
        findConnectedPrinter();
    }
    
    /**
     * Find connected USB printer (EPSON or Zebra)
     */
    private void findConnectedPrinter() {
        if (usbManager == null) {
            Log.e(TAG, "USB Manager not available");
            return;
        }
        
        connectedPrinter = null;
        printerType = PrinterType.UNKNOWN;
        
        HashMap<String, UsbDevice> deviceList = usbManager.getDeviceList();
        Log.i(TAG, "Found " + deviceList.size() + " USB devices");
        
        for (Map.Entry<String, UsbDevice> entry : deviceList.entrySet()) {
            UsbDevice device = entry.getValue();
            int vendorId = device.getVendorId();
            
            Log.d(TAG, "USB Device: " + device.getDeviceName() 
                + " VID: " + vendorId 
                + " PID: " + device.getProductId());
            
            // Check for EPSON printer (prioritize EPSON)
            if (vendorId == EPSON_VENDOR_ID) {
                connectedPrinter = device;
                printerType = PrinterType.EPSON;
                Log.i(TAG, "Found EPSON printer: " + device.getDeviceName());
                break;
            }
            
            // Check for Zebra printer
            if (vendorId == ZEBRA_VENDOR_ID) {
                connectedPrinter = device;
                printerType = PrinterType.ZEBRA;
                Log.i(TAG, "Found Zebra printer: " + device.getDeviceName());
                // Don't break - continue looking for EPSON
            }
        }
        
        if (connectedPrinter == null) {
            Log.w(TAG, "No supported printer found (EPSON or Zebra)");
        } else {
            Log.i(TAG, "Connected printer type: " + printerType);
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
     * Get the type of connected printer
     */
    public PrinterType getPrinterType() {
        return printerType;
    }
    
    /**
     * Print ESC/POS data to EPSON printer
     * 
     * @param escPosData ESC/POS commands to print
     * @return true if printing succeeded
     */
    public boolean printEscPos(String escPosData) {
        if (!isPrinterAvailable()) {
            Log.e(TAG, "No printer available");
            return false;
        }
        
        if (printerType != PrinterType.EPSON) {
            Log.w(TAG, "Connected printer is not EPSON, attempting ESC/POS anyway");
        }
        
        return printWithDirectUsb(escPosData);
    }
    
    /**
     * Print ZPL data to Zebra printer
     * 
     * @param zplData ZPL commands to print
     * @return true if printing succeeded
     */
    public boolean printZpl(String zplData) {
        if (!isPrinterAvailable()) {
            Log.e(TAG, "No printer available");
            return false;
        }
        
        if (printerType != PrinterType.ZEBRA) {
            Log.w(TAG, "Connected printer is not Zebra, ZPL may not work");
        }
        
        return printWithDirectUsb(zplData);
    }
    
    /**
     * Print receipt - automatically detects printer type and uses appropriate format
     * For EPSON: expects ESC/POS data
     * For Zebra: expects ZPL data
     * 
     * @param printData Print data (ESC/POS or ZPL depending on printer type)
     * @return true if printing succeeded
     */
    public boolean printReceipt(String printData) {
        if (!isPrinterAvailable()) {
            Log.e(TAG, "No printer available");
            return false;
        }
        
        Log.i(TAG, "Printing receipt to " + printerType + " printer");
        return printWithDirectUsb(printData);
    }
    
    /**
     * Print using direct USB communication
     */
    private boolean printWithDirectUsb(String printData) {
        UsbDeviceConnection connection = null;
        UsbInterface intf = null;
        
        try {
            Log.i(TAG, "Printing with direct USB to " + printerType + " printer...");
            
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
            UsbEndpoint endpointOut = null;
            
            for (int i = 0; i < intf.getEndpointCount(); i++) {
                UsbEndpoint ep = intf.getEndpoint(i);
                if (ep.getType() == UsbConstants.USB_ENDPOINT_XFER_BULK
                    && ep.getDirection() == UsbConstants.USB_DIR_OUT) {
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
            
            // Send print data
            byte[] data = printData.getBytes("UTF-8");
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
            status.put("printerType", printerType.toString());
            
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
        printerType = PrinterType.UNKNOWN;
    }
}
