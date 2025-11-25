// Zebra Printer Integration for Self Checkout Kiosk
// Uses Android Native Bridge (custom WebView app with Zebra Link-OS SDK)
// ES5 compatible for older kiosk browsers
// 
// REQUIREMENTS:
// - Custom Android WebView app must be deployed on the KC50 kiosk
// - The app must expose window.Android.printReceipt(zplData) JavaScript interface
// - See docs/AUTOMATIC_RECEIPT_PRINTING_SOLUTION.md for implementation details

// ZebraPrinter constructor function (ES5 compatible)
function ZebraPrinter() {
    this.androidBridgeAvailable = false;
}

/**
 * Initialize printer connection
 * Checks if the Android native bridge is available
 * @returns {Promise} Resolves to true if initialized successfully
 */
ZebraPrinter.prototype.initialize = function() {
    var self = this;
    console.log('Initializing printer via Android native bridge...');
    
    return new Promise(function(resolve) {
        // Check for Android native bridge (custom WebView app with Zebra SDK)
        if (typeof Android !== 'undefined' && typeof Android.printReceipt === 'function') {
            console.log('Android native bridge detected - printer ready');
            self.androidBridgeAvailable = true;
            resolve(true);
        } else {
            console.error('Android native bridge NOT available!');
            console.error('Please deploy the custom Android WebView app on the KC50 kiosk.');
            console.error('See docs/AUTOMATIC_RECEIPT_PRINTING_SOLUTION.md for setup instructions.');
            resolve(false);
        }
    });
};

/**
 * Check if printer is available
 */
ZebraPrinter.prototype.isPrinterAvailable = function() {
    // Check if Android bridge has printer status method
    if (typeof Android !== 'undefined' && typeof Android.isPrinterAvailable === 'function') {
        return Android.isPrinterAvailable();
    }
    return this.androidBridgeAvailable;
};

/**
 * Print receipt via Android native bridge
 * @param {string} zplCommands - ZPL commands to print
 * @returns {Promise} Resolves to result object with success status and method
 */
ZebraPrinter.prototype.printReceipt = function(zplCommands) {
    var self = this;
    console.log('Printing receipt via Android native bridge...');

    return new Promise(function(resolve) {
        if (!self.androidBridgeAvailable) {
            console.error('Cannot print - Android native bridge not available');
            resolve({ 
                success: false, 
                method: 'android-bridge', 
                error: 'Android native bridge not available. Deploy the custom WebView app.' 
            });
            return;
        }

        try {
            // Call the Android native print method
            Android.printReceipt(zplCommands);
            console.log('Print job sent to Android native bridge successfully');
            resolve({ success: true, method: 'android-bridge' });
        } catch (error) {
            console.error('Android bridge print failed:', error);
            resolve({ 
                success: false, 
                method: 'android-bridge', 
                error: error.message || 'Print failed' 
            });
        }
    });
};

/**
 * Print with callback support (if Android app supports it)
 * @param {string} zplCommands - ZPL commands to print
 * @param {function} onSuccess - Success callback
 * @param {function} onError - Error callback
 */
ZebraPrinter.prototype.printReceiptWithCallback = function(zplCommands, onSuccess, onError) {
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
};

// Global printer instance
var zebraPrinter = null;

/**
 * Initialize printer on page load
 * @returns {Promise} Resolves to true if initialized successfully
 */
function initializePrinter() {
    zebraPrinter = new ZebraPrinter();
    return zebraPrinter.initialize().then(function(initialized) {
        if (!initialized) {
            console.error('Printer initialization failed - Android bridge required');
        }
        return initialized;
    });
}

/**
 * Auto-print receipt when receipt data is available
 * Called from Success page
 * @param {string} zplCommands - ZPL commands for the receipt
 * @returns {Promise} Resolves to result object
 */
function autoPrintReceipt(zplCommands) {
    if (!zebraPrinter) {
        return initializePrinter().then(function(initialized) {
            if (!initialized) {
                console.error('Cannot auto-print: Printer initialization failed');
                return { 
                    success: false, 
                    error: 'Printer initialization failed. Deploy the custom WebView app.' 
                };
            }
            return zebraPrinter.printReceipt(zplCommands);
        });
    }

    if (!zebraPrinter.androidBridgeAvailable) {
        console.error('Cannot auto-print: Android native bridge not available');
        return Promise.resolve({ 
            success: false, 
            error: 'Android native bridge required. Deploy the custom WebView app.' 
        });
    }

    return zebraPrinter.printReceipt(zplCommands).then(function(result) {
        console.log('Auto-print result:', result);
        return result;
    }).catch(function(error) {
        console.error('Auto-print failed:', error);
        return { success: false, error: error.message };
    });
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

// Export for use in other scripts (Node.js/CommonJS compatibility)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ZebraPrinter: ZebraPrinter, initializePrinter: initializePrinter, autoPrintReceipt: autoPrintReceipt, isPrintingAvailable: isPrintingAvailable };
}
