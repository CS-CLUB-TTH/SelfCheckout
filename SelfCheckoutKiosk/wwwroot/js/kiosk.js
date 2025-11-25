// Kiosk JavaScript for Self Checkout Application
// ES5 compatible for older kiosk browsers

// Global error handler for debugging on kiosk
window.onerror = function(message, source, lineno, colno, error) {
    var errorDiv = document.getElementById('js-error-display');
    if (!errorDiv) {
        errorDiv = document.createElement('div');
        errorDiv.id = 'js-error-display';
        errorDiv.style.cssText = 'position:fixed;top:0;left:0;right:0;background:#ff0000;color:#fff;padding:10px;z-index:99999;font-size:14px;';
        document.body.appendChild(errorDiv);
    }
    errorDiv.innerHTML = 'JS Error: ' + message + ' at line ' + lineno;
    return false;
};

// Timer for auto-return to idle screen
var inactivityTimer;
var INACTIVITY_TIMEOUT = 60000; // 60 seconds

function resetInactivityTimer() {
    try {
        clearTimeout(inactivityTimer);
        inactivityTimer = setTimeout(function() {
            returnToIdle();
        }, INACTIVITY_TIMEOUT);
    } catch (e) {
        console.error('Timer error:', e);
    }
}

function returnToIdle() {
    window.location.href = '/';
}

// Initialize inactivity tracking
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Track user activity
        var events = ['click', 'touchstart', 'mousemove', 'keypress'];
        for (var i = 0; i < events.length; i++) {
            document.addEventListener(events[i], resetInactivityTimer);
        }
        
        // Start the timer
        resetInactivityTimer();
        
        // Update timer display if exists
        updateTimerDisplay();
    } catch (e) {
        console.error('Init error:', e);
    }
});

function updateTimerDisplay() {
    try {
        var timerElement = document.querySelector('.timer-display');
        if (timerElement) {
            var seconds = 60;
            setInterval(function() {
                seconds--;
                if (seconds <= 0) {
                    seconds = 60;
                }
                timerElement.textContent = 'Auto-return in: ' + seconds + 's';
            }, 1000);
        }
    } catch (e) {
        console.error('Timer display error:', e);
    }
}

// Rating selection
function selectRating(rating) {
    try {
        // Remove previous selection
        var faces = document.querySelectorAll('.rating-face');
        for (var i = 0; i < faces.length; i++) {
            faces[i].classList.remove('selected');
        }
        
        // Add selection to clicked face
        var selectedFace = document.querySelector('[data-rating="' + rating + '"]');
        if (selectedFace) {
            selectedFace.classList.add('selected');
        }
        
        // Store rating (could send to backend)
        console.log('Rating selected:', rating);
        
        // Auto-return after rating
        setTimeout(function() {
            returnToIdle();
        }, 3000);
    } catch (e) {
        console.error('Rating error:', e);
    }
}

// Payment simulation (to be replaced with actual Magnati terminal integration)
function simulatePayment() {
    try {
        // Show loading
        var paymentContent = document.querySelector('.payment-content');
        if (paymentContent) {
            paymentContent.innerHTML = '<div class="payment-icon">💳</div>' +
                '<h1 class="payment-title">Processing Payment...</h1>' +
                '<div class="spinner"></div>' +
                '<p class="payment-instruction">Please wait</p>';
        }
        
        // Simulate payment processing
        setTimeout(function() {
            window.location.href = '/Success';
        }, 3000);
    } catch (e) {
        console.error('Payment error:', e);
    }
}

// Auto-start payment on page load for payment screen
if (window.location.pathname === '/Payment') {
    setTimeout(function() {
        simulatePayment();
    }, 1000);
}
