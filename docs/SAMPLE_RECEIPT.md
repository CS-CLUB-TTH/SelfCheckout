# Sample Receipt Output

## Visual Representation of Printed Receipt

This document shows how the receipt will appear when printed on a Zebra thermal printer.

```
================================
   Self Checkout Kiosk
================================
Date: 24/11/2025 09:53:45
Transaction: TXN-20251124095345-A1B2C3D4
Customer: 12345
--------------------------------
Item                 Qty   Amount
--------------------------------
Espresso             2.00    10.00
Cappuccino           1.00     8.00
Croissant            2.00    14.00
Chocolate Muffin     1.00     6.50
Bottled Water        3.00     9.00
--------------------------------
Subtotal:                   47.50
Tax (VAT):                   2.38
================================
TOTAL:                  49.88 AED
================================
Card: VISA ****4242
Auth Code: AUTH789012
Reference: MGI-REF-456789

   Thank you for shopping!
```

## Receipt Specifications

### Paper Size
- **Width**: 58mm (thermal paper)
- **Length**: Variable (auto-calculated based on items)

### Font Specifications
- **Header**: Large, bold (40pt)
- **Body Text**: Standard (20pt)
- **Items**: Smaller (18pt)
- **Total**: Large, bold (30pt)

### Receipt Sections

1. **Header**
   - Store name (centered)
   - Decorative separator line

2. **Transaction Information**
   - Date and time
   - Transaction ID
   - Customer ID (if available)

3. **Items List**
   - Product description (left-aligned)
   - Quantity (center)
   - Amount (right-aligned)
   - Column headers
   - Separator lines

4. **Totals**
   - Subtotal (before tax)
   - Tax/VAT amount
   - **Total** (emphasized)

5. **Payment Details**
   - Card type (e.g., VISA, Mastercard)
   - Last 4 digits only (****4242)
   - Authorization code
   - Reference number

6. **Footer**
   - Thank you message (centered)

## Print Quality Features

✅ **Sharp Text**: Clean, readable fonts
✅ **Proper Spacing**: Adequate line spacing
✅ **Alignment**: Professional column alignment
✅ **Separators**: Clear section dividers
✅ **Fast Printing**: 1-2 seconds for complete receipt
✅ **No Smudging**: Thermal printing technology

## ZPL Commands Used

The receipt is generated using ZPL (Zebra Programming Language) commands:

- `^XA` / `^XZ` - Start/End format
- `^PW400` - Print width for 58mm paper
- `^FO` - Field origin (positioning)
- `^A0N` - Font selection and sizing
- `^FD` / `^FS` - Field data
- `^GB` - Graphic box (lines)

## Receipt Size

- **Typical Length**: 15-20cm (depending on number of items)
- **Print Time**: 1-2 seconds
- **Paper Usage**: Minimal (thermal paper is economical)

## Environmental Considerations

- ✅ Thermal printing uses no ink or toner
- ✅ Minimal paper waste
- ✅ Receipts can be recycled
- ✅ Optional: Digital receipt via email/SMS (future enhancement)

## Sample Receipt Variations

### Minimum Receipt (2 items)
```
================================
   Self Checkout Kiosk
================================
Date: 24/11/2025 10:15:30
Transaction: TXN-20251124101530-B2C3D4E5
--------------------------------
Item                 Qty   Amount
--------------------------------
Coffee               1.00     5.00
Donut                1.00     3.00
--------------------------------
Subtotal:                    8.00
Tax (VAT):                   0.40
================================
TOTAL:                   8.40 AED
================================
Card: MASTERCARD ****8765
Auth Code: AUTH234567

   Thank you for shopping!
```

### Maximum Receipt (10 items)
```
================================
   Self Checkout Kiosk
================================
Date: 24/11/2025 14:30:45
Transaction: TXN-20251124143045-F3G4H5I6
Customer: 67890
--------------------------------
Item                 Qty   Amount
--------------------------------
Sandwich             2.00    24.00
Salad Bowl           1.00    18.00
Fresh Juice          2.00    16.00
Chips                3.00    12.00
Chocolate Bar        2.00     8.00
Water Bottle         4.00    12.00
Yogurt               2.00    10.00
Apple                5.00    12.50
Croissant            1.00     7.00
Coffee               2.00    10.00
--------------------------------
Subtotal:                  129.50
Tax (VAT):                   6.48
================================
TOTAL:                 135.98 AED
================================
Card: VISA ****1234
Auth Code: AUTH456789
Reference: MGI-REF-123456

   Thank you for shopping!
```

## Receipt Layout Features

### Professional Appearance
- Clean, organized layout
- Consistent spacing and alignment
- Clear section separation
- Easy to read and understand

### Information Completeness
- All transaction details included
- Itemized list for transparency
- Tax breakdown shown
- Payment verification information

### Compliance
- Meets retail receipt requirements
- Includes all necessary transaction data
- Suitable for accounting/returns
- Customer copy quality

## Testing Checklist

When testing receipt printing:

- [ ] All text is readable and clear
- [ ] Amounts align properly in columns
- [ ] No text truncation or overlap
- [ ] Lines and borders appear correctly
- [ ] Card information is properly masked
- [ ] Authorization codes are correct
- [ ] Paper feeds smoothly
- [ ] Print completes in 1-2 seconds
- [ ] Receipt tears cleanly from roll
- [ ] No smudging or fading

---

**Note**: Actual receipt appearance may vary slightly based on:
- Specific Zebra printer model
- Thermal paper quality
- Printer settings and configuration
- Paper roll width (58mm standard)
