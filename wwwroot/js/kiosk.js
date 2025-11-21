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

    // Modern NFC scanner with clean error handling
    async function startNfcScan() {
        if (!statusEl) return;

        // Check NFC support
        if (!('NDEFReader' in window)) {
            updateStatus('noSupport', 'error');
            return;
        }

        try {
            updateStatus('requesting');
            const ndef = new NDEFReader();

            // Start scanning
            await ndef.scan();
            console.log('✓ NFC scan started');
            
            updateStatus('waiting', 'reading');

            // Handle scan errors
            ndef.onreadingerror = () => {
                updateStatus('error', 'error');
                setTimeout(startNfcScan, 3000); // Retry after 3 seconds
            };

            // Handle successful scan
            ndef.onreading = handleNfcRead;

        } catch (err) {
            console.error('NFC Error:', err);
            handleNfcError(err);
        }
    }

    // Handle NFC card read
    function nfcHexToDecimal(hexUID) {
        const bytes = hexUID.split(":").reverse(); // reverse for little-endian
        return parseInt(bytes.join(""), 16);
    }

    function handleNfcRead(event) {
        const { serialNumber, message } = event;

        updateStatus('detected', 'success');

        let cardIdentifier = serialNumber;

        if (!cardIdentifier && message.records.length > 0) {
            try {
                const decoder = new TextDecoder();
                cardIdentifier = message.records
                    .map(record => decoder.decode(record.data))
                    .join('');
            } catch (e) {
                console.error('Error decoding NFC message:', e);
            }
        }

        if (!cardIdentifier) {
            updateStatus('error', 'error');
            setTimeout(startNfcScan, 3000);
            return;
        }

        console.log("Raw NFC value:", cardIdentifier);

        let convertedIdentifier = cardIdentifier;

        if (/^([0-9A-F]{2}:){3,}[0-9A-F]{2}$/i.test(cardIdentifier)) {
            convertedIdentifier = nfcHexToDecimal(cardIdentifier);
            console.log("Converted NFC → Decimal:", convertedIdentifier);
        }

        else {
            console.log("Non-hex NFC value, sending as is.");
        }

        updateStatus('loading', 'success');

        // Navigate to cart
        setTimeout(() => {
            window.location.href = `/Cart?handler=LoadFromCard&cardNo=${encodeURIComponent(convertedIdentifier)}`;
        }, 800);
    }

    // Handle NFC errors
    function handleNfcError(err) {
        if (err.name === 'NotAllowedError') {
            statusEl.innerHTML = 'NFC Permission Denied<br><small>Please grant NFC access</small>';
        } else if (err.name === 'SecurityError') {
            statusEl.innerHTML = 'Security Error<br><small>HTTPS required</small>';
        } else {
            statusEl.textContent = 'NFC Error - Retrying...';
        }
        
        statusEl.classList.add('error');
        console.error('NFC Error Details:', err);
        
        // Auto-retry after 5 seconds
        setTimeout(startNfcScan, 5000);
    }

    // Update status display
    function updateStatus(messageKey, className) {
        const messages = {
            ready: 'Ready to scan...',
            requesting: 'Requesting NFC access...',
            waiting: 'Hold your card near the reader...',
            detected: 'Card detected!',
            loading: 'Loading your cart...',
            error: 'Unable to read card',
            noSupport: 'NFC not supported on this device'
        };

        if (statusEl) {
            statusEl.textContent = messages[messageKey] || messageKey;
            statusEl.className = 'nfc-status';
            if (className) statusEl.classList.add(className);
        }
    }

    // Initialize NFC on page load
    document.addEventListener('DOMContentLoaded', () => {
        console.log('NFC Page Initialized');
        startNfcScan();
    });

    // Cancel button
    cancelBtn?.addEventListener('click', () => {
        window.location.href = '/';
    });
})();