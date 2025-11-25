// Zebra Printer Integration for Self Checkout Kiosk
// Supports automatic receipt printing using:
// 1. Android Native Bridge (custom WebView app with Zebra Link-OS SDK) - RECOMMENDED
// 2. Zebra Enterprise Browser API (if working)
// 3. Zebra Browser Print API (desktop)
// 4. Browser Print Dialog (fallback for testing)

// Configuration constants
const PRINT_WINDOW_DELAY = 100; // ms to wait before printing in popup window
const PRINT_WINDOW_CLOSE_DELAY = 100; // ms to wait before closing popup after print

class ZebraPrinter {
    constructor() {
        this.defaultPrinter = null;
        this.printerName = null;
        this.browserPrintLoaded = false;
        this.zebraApiLoaded = false;
        this.androidBridgeAvailable = false;
    }

    /**
     * Initialize printer connection
     * Checks for available print APIs in order of preference
     */
    async initialize() {
        console.log('Initializing printer...');
        
        // Method 1 (PREFERRED): Android native bridge (custom WebView app with Zebra SDK)
        // This requires a custom Android app wrapping the web app
        if (typeof Android !== 'undefined' && typeof Android.printReceipt === 'function') {
            console.log('Android native bridge detected - using direct USB printing');
            this.androidBridgeAvailable = true;
            return true;
        }

        // Method 2: Zebra Enterprise Browser API (for KC50 kiosk)
        if (typeof EB !== 'undefined' && EB.Printer) {
            console.log('Zebra Enterprise Browser API detected');
            this.zebraApiLoaded = true;
            return true;
        }

        // Method 3: Zebra Browser Print API (desktop)
        if (typeof BrowserPrint !== 'undefined') {
            console.log('Zebra Browser Print API detected');
            this.browserPrintLoaded = true;
            
            try {
                await this.getDefaultPrinter();
                return true;
            } catch (error) {
                console.error('Failed to get default printer:', error);
            }
        }

        console.warn('No direct printer API detected. For KC50 kiosk, deploy the custom Android WebView app.');
        console.warn('See docs/AUTOMATIC_RECEIPT_PRINTING_SOLUTION.md for implementation guide.');
        return false;
    }

    /**
     * Check if running on Android
     */
    isAndroid() {
        return /Android/i.test(navigator.userAgent);
    }

    /**
     * Get default printer using Browser Print API
     */
    async getDefaultPrinter() {
        return new Promise((resolve, reject) => {
            if (!this.browserPrintLoaded) {
                reject('Browser Print not loaded');
                return;
            }

            BrowserPrint.getDefaultDevice('printer', (device) => {
                if (device) {
                    this.defaultPrinter = device;
                    this.printerName = device.name;
                    console.log('Default printer:', this.printerName);
                    resolve(device);
                } else {
                    console.warn('No default printer found');
                    reject('No default printer found');
                }
            }, (error) => {
                console.error('Error getting default printer:', error);
                reject(error);
            });
        });
    }

    /**
     * Print ZPL commands using Browser Print API
     */
    async printZplBrowserPrint(zplCommands) {
        if (!this.browserPrintLoaded || !this.defaultPrinter) {
            throw new Error('Browser Print not initialized');
        }

        return new Promise((resolve, reject) => {
            this.defaultPrinter.send(zplCommands, 
                () => {
                    console.log('ZPL sent to printer successfully');
                    resolve();
                },
                (error) => {
                    console.error('Error sending ZPL to printer:', error);
                    reject(error);
                }
            );
        });
    }

    /**
     * Print ZPL commands using Enterprise Browser API
     */
    async printZplEnterpriseBrowser(zplCommands) {
        if (!this.zebraApiLoaded) {
            throw new Error('Zebra Enterprise Browser API not available');
        }

        return new Promise((resolve, reject) => {
            try {
                // Connect to printer
                EB.Printer.connectPrinter(() => {
                    console.log('Connected to printer');
                    
                    // Send ZPL data
                    EB.Printer.printRawString(zplCommands, () => {
                        console.log('Print job sent successfully');
                        resolve();
                    }, (error) => {
                        console.error('Error printing:', error);
                        reject(error);
                    });
                }, (error) => {
                    console.error('Error connecting to printer:', error);
                    reject(error);
                });
            } catch (error) {
                console.error('Exception in printZplEnterpriseBrowser:', error);
                reject(error);
            }
        });
    }

    /**
     * Print receipt automatically
     * Tries available methods in order of preference
     * @param {string} zplCommands - ZPL commands to print
     * @param {string} htmlReceipt - HTML fallback for browser print
     */
    async printReceipt(zplCommands, htmlReceipt) {
        console.log('Attempting to print receipt...');

        try {
            // Method 1 (PREFERRED): Android native bridge with Zebra SDK
            if (this.androidBridgeAvailable) {
                console.log('Using Android native bridge (Zebra SDK)');
                try {
                    Android.printReceipt(zplCommands);
                    return { success: true, method: 'android-bridge' };
                } catch (error) {
                    console.error('Android bridge print failed:', error);
                }
            }

            // Method 2: Zebra Enterprise Browser API
            if (this.zebraApiLoaded) {
                console.log('Using Zebra Enterprise Browser API');
                try {
                    await this.printZplEnterpriseBrowser(zplCommands);
                    return { success: true, method: 'zebra-enterprise' };
                } catch (error) {
                    console.error('Zebra Enterprise Browser print failed:', error);
                }
            }

            // Method 3: Zebra Browser Print API
            if (this.browserPrintLoaded && this.defaultPrinter) {
                console.log('Using Zebra Browser Print API');
                try {
                    await this.printZplBrowserPrint(zplCommands);
                    return { success: true, method: 'browser-print' };
                } catch (error) {
                    console.error('Browser Print API failed:', error);
                }
            }

            // Method 4: Fallback to browser print dialog with HTML (for testing only)
            if (this.isAndroid()) {
                console.warn('No print API available on Android. Deploy the custom WebView app for automatic printing.');
                console.warn('See docs/AUTOMATIC_RECEIPT_PRINTING_SOLUTION.md');
                return { success: false, method: 'none', error: 'No Android print bridge available' };
            }

            console.log('Falling back to browser print dialog (testing mode)');
            this.printHtmlFallback(htmlReceipt);
            return { success: true, method: 'browser-fallback' };

        } catch (error) {
            console.error('Error printing receipt:', error);
            
            // Last resort: browser print dialog
            console.log('Using fallback browser print');
            this.printHtmlFallback(htmlReceipt);
            return { success: false, method: 'fallback', error: error.message };
        }
    }

    /**
     * Fallback to browser print dialog with HTML
     */
    printHtmlFallback(htmlReceipt) {
        const printWindow = window.open('', '_blank', 'width=400,height=600');
        if (printWindow) {
            printWindow.document.write(htmlReceipt);
            printWindow.document.close();
            
            // Auto-print after content loads
            printWindow.onload = () => {
                setTimeout(() => {
                    printWindow.print();
                    // Close after print dialog
                    setTimeout(() => printWindow.close(), PRINT_WINDOW_CLOSE_DELAY);
                }, PRINT_WINDOW_DELAY);
            };
        } else {
            console.error('Failed to open print window - popup blocked?');
            alert('Unable to print receipt. Please check popup blocker settings.');
        }
    }

    /**
     * Print plain text to any printer using raw printing
     */
    async printPlainText(text) {
        console.log('Printing plain text...');
        
        try {
            if (this.browserPrintLoaded && this.defaultPrinter) {
                return new Promise((resolve, reject) => {
                    this.defaultPrinter.send(text,
                        () => {
                            console.log('Plain text sent to printer');
                            resolve();
                        },
                        (error) => {
                            console.error('Error sending plain text:', error);
                            reject(error);
                        }
                    );
                });
            }
        } catch (error) {
            console.error('Error printing plain text:', error);
            throw error;
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
    await zebraPrinter.initialize();
}

/**
 * Auto-print receipt when receipt data is available
 * Called from Success page
 */
async function autoPrintReceipt(zplCommands, htmlReceipt) {
    if (!zebraPrinter) {
        await initializePrinter();
    }

    try {
        const result = await zebraPrinter.printReceipt(zplCommands, htmlReceipt);
        console.log('Print result:', result);
        return result;
    } catch (error) {
        console.error('Auto-print failed:', error);
        return { success: false, error: error.message };
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ZebraPrinter, initializePrinter, autoPrintReceipt };
}
