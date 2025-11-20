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
