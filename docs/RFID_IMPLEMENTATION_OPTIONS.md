# RFID Implementation Options - Quick Reference

## TL;DR - What Should I Do?

### Step 1: Identify Your Card Type
```bash
# Check your physical RFID cards for these markings:
# - "13.56 MHz" or "HF" → You have HF RFID cards
# - "860-960 MHz" or "UHF" → You have UHF RFID cards
# - "NFC", "ISO 14443", "MIFARE" → You have HF RFID cards (NFC-compatible)
```

### Step 2: Choose Implementation

#### If HF Cards (13.56 MHz):
```
✅ KEEP CURRENT IMPLEMENTATION - IT ALREADY WORKS!
```
Your current Web NFC code in `/wwwroot/js/kiosk.js` reads HF RFID cards.

#### If UHF Cards (860-960 MHz):
```
❌ CURRENT IMPLEMENTATION WON'T WORK
✅ FOLLOW "UHF Implementation Guide" BELOW
```

---

## Option A: Current Web NFC Implementation (HF RFID)

### What It Reads
- ✅ NFC tags (Type 1, 2, 3, 4, 5)
- ✅ ISO 14443-A cards (MIFARE Classic, MIFARE Ultralight, MIFARE DESFire)
- ✅ ISO 14443-B cards
- ✅ ISO 15693 cards (NFC-V)
- ✅ FeliCa cards
- ✅ All 13.56 MHz HF RFID cards

### Browser Requirements
```
Chrome 89+ on Android
HTTPS required
NFC permission granted
```

### How It Works (Current Code)
```javascript
// Location: /wwwroot/js/kiosk.js (lines 124-248)

// Initialize NFC reader
const ndef = new NDEFReader();
await ndef.scan();

// Handle card tap
ndef.onreading = (event) => {
    const cardNo = event.serialNumber; // This is the card_no
    window.location.href = `/Cart/LoadFromCard?cardNo=${cardNo}`;
};
```

### Testing Current Implementation
1. Navigate to `/Nfc` page on Zebra KC50
2. Tap your RFID card
3. If it works → You have HF cards (done!)
4. If it doesn't work → You have UHF cards (see Option B)

### Advantages
- ✅ Already implemented
- ✅ No additional hardware
- ✅ No native app needed
- ✅ Simple maintenance
- ✅ Browser-based

### Limitations
- ❌ 4cm range only
- ❌ Requires HTTPS
- ❌ Chrome Android only
- ❌ Cannot read UHF cards

---

## Option B: Native Android + UHF RFID SDK

### When to Use
- You have UHF RFID cards (860-960 MHz)
- You need longer read range (5-20 meters)
- You need to read multiple cards simultaneously

### Hardware Required
- Zebra KC50 kiosk ✅ (you have this)
- Zebra UHF RFID reader (e.g., RFD8500, RFD40) 🛒 (need to purchase)

### Architecture
```
┌─────────────────────────────────────────┐
│  Web Browser (Chrome)                   │
│  └─ JavaScript App (your current code)  │
└─────────────┬───────────────────────────┘
              │ HTTP Request
              ↓
┌─────────────────────────────────────────┐
│  Native Android App (NEW)               │
│  ├─ Local HTTP Server (port 8080)      │
│  ├─ Zebra RFID SDK                      │
│  └─ RFID Reader Interface               │
└─────────────┬───────────────────────────┘
              │ USB/Bluetooth
              ↓
┌─────────────────────────────────────────┐
│  Zebra UHF RFID Reader                  │
│  (RFD8500, RFD40, etc.)                 │
└─────────────────────────────────────────┘
```

### Implementation Steps

#### 1. Create Native Android App

**File**: `RFIDScannerActivity.java`
```java
import com.zebra.rfid.api3.*;
import fi.iki.elonen.NanoHTTPD;

public class RFIDScannerActivity extends AppCompatActivity {
    private RFIDReader reader;
    private SimpleHTTPServer server;
    private String lastCardNo = "";
    
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        // Initialize RFID reader
        initializeRFIDReader();
        
        // Start HTTP server
        server = new SimpleHTTPServer(8080);
        try {
            server.start();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
    
    private void initializeRFIDReader() {
        Readers readers = new Readers(this, ENUM_TRANSPORT.ALL);
        ArrayList<ReaderDevice> availableReaders = readers.GetAvailableRFIDReaderList();
        
        if (availableReaders.size() > 0) {
            reader = availableReaders.get(0).getRFIDReader();
            try {
                reader.connect();
                configureReader();
                startInventory();
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }
    
    private void configureReader() throws Exception {
        // Configure reader settings
        reader.Config.setStartTrigger(new TriggerInfo());
        reader.Config.setStopTrigger(new TriggerInfo());
    }
    
    private void startInventory() {
        reader.Events.addEventsListener(new RfidEventsListener() {
            @Override
            public void eventReadNotify(RfidReadEvents e) {
                TagData[] tags = reader.Actions.getReadTags(100);
                if (tags != null && tags.length > 0) {
                    lastCardNo = tags[0].getTagID();
                    Log.d("RFID", "Card detected: " + lastCardNo);
                }
            }
            
            @Override
            public void eventStatusNotify(RfidStatusEvents e) {
                // Handle status events
            }
        });
        
        try {
            reader.Actions.Inventory.perform();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
    // Simple HTTP Server
    private class SimpleHTTPServer extends NanoHTTPD {
        public SimpleHTTPServer(int port) {
            super(port);
        }
        
        @Override
        public Response serve(IHTTPSession session) {
            String uri = session.getUri();
            
            if ("/scan".equals(uri)) {
                // Return last scanned card
                String json = "{\"cardNo\":\"" + lastCardNo + "\",\"success\":" + 
                             (!lastCardNo.isEmpty()) + "}";
                return newFixedLengthResponse(
                    Response.Status.OK,
                    "application/json",
                    json
                );
            } else if ("/status".equals(uri)) {
                // Return reader status
                boolean connected = reader != null && reader.isConnected();
                String json = "{\"connected\":" + connected + "}";
                return newFixedLengthResponse(
                    Response.Status.OK,
                    "application/json",
                    json
                );
            }
            
            return newFixedLengthResponse(Response.Status.NOT_FOUND, "text/plain", "Not found");
        }
    }
    
    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (reader != null) {
            try {
                reader.disconnect();
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
        if (server != null) {
            server.stop();
        }
    }
}
```

**File**: `AndroidManifest.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.yourcompany.rfidscanner">
    
    <!-- Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.BLUETOOTH" />
    <uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
    <uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
    <uses-permission android:name="android.permission.USB_PERMISSION" />
    
    <application
        android:allowBackup="true"
        android:icon="@mipmap/ic_launcher"
        android:label="@string/app_name"
        android:theme="@style/AppTheme">
        
        <activity
            android:name=".RFIDScannerActivity"
            android:exported="true"
            android:launchMode="singleTask">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>
    </application>
</manifest>
```

**File**: `build.gradle`
```gradle
dependencies {
    implementation 'com.zebra:RFIDAPI3:1.2.0' // Check latest version
    implementation 'org.nanohttpd:nanohttpd:2.3.1'
    implementation 'androidx.appcompat:appcompat:1.6.1'
}
```

#### 2. Update JavaScript Code

**File**: `/wwwroot/js/kiosk-rfid.js` (NEW FILE)
```javascript
// RFID handling for UHF readers via native Android app
(function () {
    if (window.location.pathname !== '/Nfc') return;

    const statusEl = document.getElementById('nfcStatus');
    const cancelBtn = document.getElementById('nfcCancel');
    let scanInterval;
    
    // Configure API endpoint (default: localhost:8080)
    // Can be overridden via window.RFID_API_BASE_URL
    const API_BASE_URL = window.RFID_API_BASE_URL || 'http://localhost:8080';

    // Check if native RFID app is running
    async function checkNativeAppStatus() {
        try {
            const response = await fetch(`${API_BASE_URL}/status`, {
                method: 'GET',
                cache: 'no-cache'
            });
            const data = await response.json();
            return data.connected;
        } catch (error) {
            return false;
        }
    }

    // Start scanning for RFID cards
    async function startRfidScan() {
        updateStatus('requesting');
        
        // Verify native app is running
        const appRunning = await checkNativeAppStatus();
        if (!appRunning) {
            updateStatus('error', 'error');
            statusEl.innerHTML = 'RFID Scanner Not Ready<br><small>Please ensure RFID app is running</small>';
            return;
        }

        updateStatus('waiting', 'reading');
        
        // Poll for card scans
        scanInterval = setInterval(async () => {
            try {
                const response = await fetch(`${API_BASE_URL}/scan`, {
                    method: 'GET',
                    cache: 'no-cache'
                });
                const data = await response.json();
                
                if (data.success && data.cardNo) {
                    clearInterval(scanInterval);
                    handleCardDetected(data.cardNo);
                }
            } catch (error) {
                console.error('Scan error:', error);
            }
        }, 500); // Poll every 500ms
    }

    // Handle card detection
    function handleCardDetected(cardNo) {
        console.log('✓ RFID Card detected:', cardNo);
        updateStatus('detected', 'success');
        
        setTimeout(() => {
            updateStatus('loading', 'success');
            window.location.href = `/Cart/LoadFromCard?cardNo=${encodeURIComponent(cardNo)}`;
        }, 800);
    }

    // Update status display
    function updateStatus(messageKey, className) {
        const messages = {
            ready: 'Ready to scan...',
            requesting: 'Connecting to RFID reader...',
            waiting: 'Hold your card near the reader...',
            detected: 'Card detected!',
            loading: 'Loading your cart...',
            error: 'Unable to read card'
        };

        if (statusEl) {
            statusEl.textContent = messages[messageKey] || messageKey;
            statusEl.className = 'nfc-status';
            if (className) statusEl.classList.add(className);
        }
    }

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', () => {
        console.log('RFID Page Initialized (UHF Mode)');
        startRfidScan();
    });

    // Cancel button
    cancelBtn?.addEventListener('click', () => {
        clearInterval(scanInterval);
        window.location.href = '/';
    });

    // Cleanup on page unload
    window.addEventListener('beforeunload', () => {
        clearInterval(scanInterval);
    });
})();
```

#### 3. Update Page to Use New Script

**File**: `/Pages/Nfc.cshtml`
```cshtml
@page
@model NfcModel
@{
    ViewData["Title"] = "Tap RFID Card";
    Layout = "_KioskLayout";
}

<div class="nfc-screen">
    <div class="nfc-panel">
        <div class="nfc-animation-container">
            <img src="~/nfc-tap-animation.svg" alt="Tap your RFID card" class="nfc-animation" />
        </div>
        <h1 class="nfc-title">Tap Your Card</h1>
        <p class="nfc-instruction">Hold your RFID card near the reader</p>
        <div class="nfc-status" id="nfcStatus">Ready to scan...</div>
        <button type="button" class="nfc-cancel-button" id="nfcCancel">Cancel</button>
    </div>
</div>

@section Scripts {
    @* Use UHF RFID script instead of Web NFC *@
    <script src="~/js/kiosk-rfid.js"></script>
}
```

### Deployment Steps

1. **Install Zebra RFID SDK**
   ```bash
   # Download from: https://www.zebra.com/us/en/support-downloads/software/rfid-software/rfid-sdk-for-android.html
   # Add to your Android project dependencies
   ```

2. **Build Android App**
   ```bash
   ./gradlew assembleRelease
   ```

3. **Install on KC50**
   ```bash
   adb install app-release.apk
   ```

4. **Configure Auto-Start**
   - Set Android app to start on boot
   - Ensure it starts before web browser

5. **Deploy Web App**
   ```bash
   dotnet publish -c Release
   # Deploy to KC50
   ```

6. **Test**
   - Start native Android app
   - Open web browser to your application
   - Navigate to /Nfc
   - Test card scanning

### Advantages
- ✅ Supports UHF RFID (long range)
- ✅ Can read multiple cards
- ✅ Better performance
- ✅ No browser limitations

### Disadvantages
- ❌ Requires UHF RFID reader ($500-2000)
- ❌ Complex architecture
- ❌ More maintenance
- ❌ Native Android development

---

## Option C: DataWedge Integration

### When to Use
- You want to use Zebra's built-in DataWedge service
- You need minimal native code
- You want to support both barcode and RFID

### Quick Setup

1. **Configure DataWedge**
   - Open DataWedge app on KC50
   - Create profile: "SelfCheckoutRFID"
   - Enable RFID input
   - Set Intent action: `com.selfcheckout.RFID_SCAN`

2. **Create Android Receiver**
   ```java
   public class DataWedgeReceiver extends BroadcastReceiver {
       @Override
       public void onReceive(Context context, Intent intent) {
           String data = intent.getStringExtra("com.symbol.datawedge.data_string");
           // Send to web view
       }
   }
   ```

3. **Register in Manifest**
   ```xml
   <receiver android:name=".DataWedgeReceiver" android:exported="true">
       <intent-filter>
           <action android:name="com.selfcheckout.RFID_SCAN" />
       </intent-filter>
   </receiver>
   ```

---

## Decision Matrix

| Requirement | Option A (Web NFC) | Option B (Native UHF) | Option C (DataWedge) |
|-------------|-------------------|----------------------|---------------------|
| **HF Cards (13.56 MHz)** | ✅ Perfect | ⚠️ Overkill | ⚠️ Overkill |
| **UHF Cards (860-960 MHz)** | ❌ Won't work | ✅ Perfect | ✅ Good |
| **Long Range (>4cm)** | ❌ No | ✅ Yes (20m) | ✅ Yes |
| **Browser-Based** | ✅ Yes | ⚠️ Hybrid | ⚠️ Hybrid |
| **No Native Code** | ✅ None | ❌ Significant | ⚠️ Minimal |
| **Cost** | ✅ $0 | ❌ $2500-7000 | ⚠️ $500-3000 |
| **Maintenance** | ✅ Easy | ❌ Complex | ⚠️ Medium |
| **Setup Time** | ✅ Done | ❌ 2-4 weeks | ⚠️ 1 week |

## Recommendation Algorithm

```
IF your_cards_are_13.56_MHz THEN
    Use Option A (Current Web NFC) ← **RECOMMENDED**
ELSE IF your_cards_are_UHF AND you_need_long_range THEN
    Use Option B (Native UHF RFID)
ELSE IF you_want_zebra_ecosystem AND have_datawedge THEN
    Use Option C (DataWedge)
ELSE
    Test Option A first, then decide
END IF
```

## Next Steps

1. **Identify your card type**
   - Check physical card for frequency marking
   - Test with current Web NFC implementation

2. **Choose implementation**
   - HF cards → Keep current (Option A)
   - UHF cards → Implement Option B
   - DataWedge → Implement Option C

3. **Implement and test**
   - Follow relevant section above
   - Test thoroughly with actual cards
   - Deploy to production

## Support Resources

- **Zebra Developer Portal**: https://techdocs.zebra.com/
- **RFID SDK Downloads**: https://www.zebra.com/us/en/support-downloads/software/rfid-software/
- **Sample Projects**: https://github.com/felixchiuman/RFID-Zebra-Sample
- **Support Community**: https://supportcommunity.zebra.com/

---

**Quick Start Completed! Choose your path and implement.** 🚀
