// Kiosk JavaScript for Self Checkout Application

// Timer for auto-return to idle screen
let inactivityTimer;
const INACTIVITY_TIMEOUT = 60000; // 60 seconds

function resetInactivityTimer() {
    clearTimeout(inactivityTimer);
    inactivityTimer = setTimeout(() => {
        returnToIdle();
    }, INACTIVITY_TIMEOUT);
}

function returnToIdle() {
    window.location.href = '/';
}

// Initialize inactivity tracking
document.addEventListener('DOMContentLoaded', () => {
    // Track user activity
    //['click', 'touchstart', 'mousemove', 'keypress'].forEach(event => {
    //    document.addEventListener(event, resetInactivityTimer);
    //});
    
    // Start the timer
    /*resetInactivityTimer();*/
    
    // Update timer display if exists
    updateTimerDisplay();

    initializeCartButtons();
});

function updateTimerDisplay() {
    const timerElement = document.querySelector('.timer-display');
    if (timerElement) {
        let seconds = 60;
        setInterval(() => {
            seconds--;
            if (seconds <= 0) {
                seconds = 60;
            }
            timerElement.textContent = `Auto-return in: ${seconds}s`;
        }, 1000);
    }
}

function initializeCartButtons() {
    const cancelButton = document.querySelector('.btn-cancel');
    const proceedButton = document.querySelector('.btn-proceed');

    if (cancelButton) {
        cancelButton.addEventListener('click', () => {
            returnToIdle();
        });
    }

    if (proceedButton) {
        proceedButton.addEventListener('click', () => {
            window.location.href = '/Payment';
        });
    }
}

// Rating selection
function selectRating(rating) {
    // Remove previous selection
    document.querySelectorAll('.rating-face').forEach(face => {
        face.classList.remove('selected');
    });
    
    // Add selection to clicked face
    const selectedFace = document.querySelector(`[data-rating="${rating}"]`);
    if (selectedFace) {
        selectedFace.classList.add('selected');
    }
    
    // Store rating (could send to backend)
    console.log('Rating selected:', rating);
    
    // Auto-return after rating
    setTimeout(() => {
        returnToIdle();
    }, 3000);
}

// Payment simulation (to be replaced with actual Magnati terminal integration)
function simulatePayment() {
    // Show loading
    const paymentContent = document.querySelector('.payment-content');
    if (paymentContent) {
        paymentContent.innerHTML = `
            <div class="payment-icon">
                <img src="/card-icon.svg" alt="Payment" style="width: 180px; height: 180px; filter: drop-shadow(0 10px 30px rgba(0, 0, 0, 0.3));" />
            </div>
            <h1 class="payment-title">Processing Payment...</h1>
            <div class="spinner"></div>
            <p class="payment-instruction">Please wait</p>
        `;
    }
    
    // Simulate payment processing - reduced to 2 seconds for better UX
    setTimeout(() => {
        window.location.href = '/Success';
    }, 2000);
}

// Auto-start payment on page load for payment screen
if (window.location.pathname === '/Payment') {
    // Reduced delay to 500ms for immediate feedback
    setTimeout(() => {
        simulatePayment();
    }, 500);
}

// Register service worker for PWA installability/offline
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' })
            .catch(err => console.error('SW registration failed', err));
    });
}

// NFC handling for dedicated /Nfc page
(function () {
    if (window.location.pathname !== '/Nfc') return;

    const statusEl = document.getElementById('nfcStatus');
    const cancelBtn = document.getElementById('nfcCancel');

    async function startNfcScan() {
        if (!statusEl) return;

        if (!('NDEFReader' in window)) {
            statusEl.textContent = 'NFC not supported on this device';
            statusEl.classList.add('error');
            return;
        }

        try {
            statusEl.textContent = 'Requesting NFC access...';

            // Explicit permission request for kiosk
            const ndef = new NDEFReader();

            // Method 1: Try with explicit permission request
            try {
                // This should trigger the permission prompt
                await ndef.scan();
                console.log('NFC permission granted');
            } catch (scanError) {
                console.log('First scan attempt failed:', scanError);

                // Method 2: If first attempt fails, try with user gesture simulation
                statusEl.textContent = 'Please tap to enable NFC...';

                // Create a button to trigger NFC with user gesture
                const enableButton = document.createElement('button');
                enableButton.textContent = 'ENABLE NFC';
                enableButton.style.cssText = `
                position: fixed;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                padding: 20px;
                font-size: 18px;
                background: #007bff;
                color: white;
                border: none;
                border-radius: 5px;
                z-index: 10000;
            `;

                await new Promise((resolve) => {
                    enableButton.onclick = async () => {
                        document.body.removeChild(enableButton);
                        try {
                            await ndef.scan();
                            resolve();
                        } catch (e) {
                            throw e;
                        }
                    };
                    document.body.appendChild(enableButton);
                });
            }

            statusEl.textContent = 'Hold card near reader...';
            statusEl.classList.add('reading');

            // Rest of your NFC reading logic
            ndef.onreadingerror = () => {
                statusEl.textContent = 'Read error. Try again';
                statusEl.classList.remove('reading');
                statusEl.classList.add('error');
            };

            ndef.onreading = (event) => {
                const { serialNumber, message } = event;
                statusEl.textContent = 'Card detected - Loading products...';
                statusEl.classList.add('success');

                // Extract card data
                let cardData = serialNumber; // Primary identifier
                let payloadText = '';

                for (const record of message.records) {
                    try {
                        const textDecoder = new TextDecoder(record.encoding || 'utf-8');
                        payloadText += textDecoder.decode(record.data);
                    } catch { }
                }

                // Use the card data (serial number or payload) as identifier
                const cardIdentifier = cardData || payloadText;
                
                console.log('NFC Card detected:', cardIdentifier);

                // Redirect to cart with card identifier
                setTimeout(() => {
                    window.location.href = `/Cart/LoadFromCard?cardNo=${encodeURIComponent(cardIdentifier)}`;
                }, 1000);
            };


        } catch (err) {
            console.error('NFC Blocked Error:', err);

            // Provide specific troubleshooting steps
            statusEl.innerHTML = 'NFC Blocked<br><small>See console for details</small>';
            statusEl.classList.add('error');

            // Detailed error information
            if (err.name === 'NotAllowedError') {
                console.error('🔒 NFC Permission Denied');
                console.error('Solution: Grant NFC permission in browser settings');
            } else if (err.name === 'SecurityError') {
                console.error('🔒 Security Error - Possible issues:');
                console.error('1. Not HTTPS (current:', window.location.protocol, ')');
                console.error('2. Invalid SSL certificate');
                console.error('3. Page not served from secure origin');
            }

            // Auto-retry after 5 seconds
            setTimeout(startNfcScan, 5000);
        }
    }

    // Kiosk initialization function
    function initializeKioskNFC() {
        console.log('Initializing Kiosk NFC Mode');

        // Start NFC automatically in kiosk mode
        startNfcScan();

        // Add periodic health check for kiosk mode
        setInterval(() => {
            if (!statusEl.classList.contains('reading') &&
                !statusEl.classList.contains('success')) {
                console.log('Kiosk NFC health check - restarting scan');
                startNfcScan();
            }
        }, 60000); // Check every minute
    }

    // Start when page loads in kiosk mode
    document.addEventListener('DOMContentLoaded', function () {
        console.log('Kiosk NFC Page Loaded');
        initializeKioskNFC();
    });

    cancelBtn?.addEventListener('click', () => {
        window.location.href = '/';
    });

    startNfcScan();
})();