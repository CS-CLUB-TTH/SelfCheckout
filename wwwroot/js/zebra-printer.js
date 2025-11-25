// Zebra Printer Integration for Self Checkout Kiosk
// Uses Android Native Bridge (custom WebView app with Zebra Link-OS SDK)
// 
// REQUIREMENTS:
// - Custom Android WebView app must be deployed on the KC50 kiosk
// - The app must expose window.Android.printReceipt(zplData) JavaScript interface
// - See docs/AUTOMATIC_RECEIPT_PRINTING_SOLUTION.md for implementation details

class ZebraPrinter {
    constructor() {
        this.androidBridgeAvailable = false;
    }

    /**
     * Initialize printer connection
     * Checks if the Android native bridge is available
     */
    async initialize() {
        console.log('Initializing printer via Android native bridge...');
        
        // Check for Android native bridge (custom WebView app with Zebra SDK)
        if (typeof Android !== 'undefined' && typeof Android.printReceipt === 'function') {
            console.log('Android native bridge detected - printer ready');
            this.androidBridgeAvailable = true;
            return true;
        }

        console.error('Android native bridge NOT available!');
        console.error('Please deploy the custom Android WebView app on the KC50 kiosk.');
        console.error('See docs/AUTOMATIC_RECEIPT_PRINTING_SOLUTION.md for setup instructions.');
        return false;
    }

    /**
     * Check if printer is available
     */
    isPrinterAvailable() {
        // Check if Android bridge has printer status method
        if (typeof Android !== 'undefined' && typeof Android.isPrinterAvailable === 'function') {
            return Android.isPrinterAvailable();
        }
        return this.androidBridgeAvailable;
    }

    /**
     * Print receipt via Android native bridge
     * @param {string} zplCommands - ZPL commands to print
     * @returns {object} Result object with success status and method
     */
    async printReceipt(zplCommands) {
        console.log('Printing receipt via Android native bridge...');

        if (!this.androidBridgeAvailable) {
            console.error('Cannot print - Android native bridge not available');
            return { 
                success: false, 
                method: 'android-bridge', 
                error: 'Android native bridge not available. Deploy the custom WebView app.' 
            };
        }

        try {
            // Call the Android native print method
            Android.printReceipt(zplCommands);
            console.log('Print job sent to Android native bridge successfully');
            return { success: true, method: 'android-bridge' };
        } catch (error) {
            console.error('Android bridge print failed:', error);
            return { 
                success: false, 
                method: 'android-bridge', 
                error: error.message || 'Print failed' 
            };
        }
    }

    /**
     * Print with callback support (if Android app supports it)
     * @param {string} zplCommands - ZPL commands to print
     * @param {function} onSuccess - Success callback
     * @param {function} onError - Error callback
     */
    printReceiptWithCallback(zplCommands, onSuccess, onError) {
        if (!this.androidBridgeAvailable) {
            if (onError) {
                onError('Android native bridge not available');
            }
            return;
        }

        try {
            // If Android app supports callback-based printing
            if (typeof Android.printReceiptWithCallback === 'function') {
                Android.printReceiptWithCallback(zplCommands, onSuccess, onError);
            } else {
                // Fallback to simple print
                Android.printReceipt(zplCommands);
                if (onSuccess) {
                    onSuccess();
                }
            }
        } catch (error) {
            console.error('Print error:', error);
            if (onError) {
                onError(error.message || 'Print failed');
            }
        }
    }
}

// Global printer instance
let zebraPrinter = null;

/**
 * Initialize printer on page load
 */
async function initializePrinter() {
    zebraPrinter = new ZebraPrinter();
    const initialized = await zebraPrinter.initialize();
    
    if (!initialized) {
        console.error('Printer initialization failed - Android bridge required');
    }
    
    return initialized;
}

/**
 * Auto-print receipt when receipt data is available
 * Called from Success page
 * @param {string} zplCommands - ZPL commands for the receipt
 */
async function autoPrintReceipt(zplCommands) {
    if (!zebraPrinter) {
        const initialized = await initializePrinter();
        if (!initialized) {
            console.error('Cannot auto-print: Printer initialization failed');
            return { 
                success: false, 
                error: 'Printer initialization failed. Deploy the custom WebView app.' 
            };
        }
    }

    if (!zebraPrinter.androidBridgeAvailable) {
        console.error('Cannot auto-print: Android native bridge not available');
        return { 
            success: false, 
            error: 'Android native bridge required. Deploy the custom WebView app.' 
        };
    }

    try {
        const result = await zebraPrinter.printReceipt(zplCommands);
        console.log('Auto-print result:', result);
        return result;
    } catch (error) {
        console.error('Auto-print failed:', error);
        return { success: false, error: error.message };
    }
}

/**
 * Check if printing is available
 */
function isPrintingAvailable() {
    if (!zebraPrinter) {
        return typeof Android !== 'undefined' && typeof Android.printReceipt === 'function';
    }
    return zebraPrinter.isPrinterAvailable();
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ZebraPrinter, initializePrinter, autoPrintReceipt, isPrintingAvailable };
}
