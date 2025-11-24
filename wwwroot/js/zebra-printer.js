// Zebra Printer Integration for Self Checkout Kiosk
// Supports automatic receipt printing using Zebra Browser Print API

class ZebraPrinter {
    constructor() {
        this.defaultPrinter = null;
        this.printerName = null;
        this.browserPrintLoaded = false;
        this.zebraApiLoaded = false;
    }

    /**
     * Initialize printer connection
     * Checks for Zebra Browser Print API or Enterprise Browser API
     */
    async initialize() {
        console.log('Initializing Zebra printer...');
        
        // Check if Zebra Enterprise Browser API is available (for KC50 kiosk)
        if (typeof EB !== 'undefined' && EB.Printer) {
            console.log('Zebra Enterprise Browser API detected');
            this.zebraApiLoaded = true;
            return true;
        }

        // Check if Browser Print is available
        if (typeof BrowserPrint !== 'undefined') {
            console.log('Zebra Browser Print API detected');
            this.browserPrintLoaded = true;
            
            try {
                // Get default printer
                await this.getDefaultPrinter();
                return true;
            } catch (error) {
                console.error('Failed to get default printer:', error);
                return false;
            }
        }

        console.warn('No Zebra printer API detected. Will fall back to browser print.');
        return false;
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
     * @param {string} zplCommands - ZPL commands to print
     * @param {string} htmlReceipt - HTML fallback for browser print
     */
    async printReceipt(zplCommands, htmlReceipt) {
        console.log('Attempting to print receipt...');

        try {
            // Try Zebra-specific methods first
            if (this.zebraApiLoaded) {
                console.log('Using Zebra Enterprise Browser API');
                await this.printZplEnterpriseBrowser(zplCommands);
                return { success: true, method: 'zebra-enterprise' };
            }

            if (this.browserPrintLoaded && this.defaultPrinter) {
                console.log('Using Zebra Browser Print API');
                await this.printZplBrowserPrint(zplCommands);
                return { success: true, method: 'browser-print' };
            }

            // Fallback to browser print dialog with HTML
            console.log('Falling back to browser print dialog');
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
                    setTimeout(() => printWindow.close(), 100);
                }, 100);
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
