-- =============================================
-- Stored Procedure: p_complete_selfcheckout_payment
-- Purpose: Complete payment for Self Checkout Kiosk transactions
-- Description: Updates bill header with payment information and inserts payment record
-- 
-- Usage: 
--   EXEC [dbo].[p_complete_selfcheckout_payment] 
--       @bill_hdr_key = 12345,
--       @payment_type_id = 2,
--       @credit_card_type_id = 1,
--       @authorization_code = 'AUTH123456',
--       @reference_number = 'REF789012'
-- =============================================

USE [CUBES]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[p_complete_selfcheckout_payment]
(
    @bill_hdr_key INT,
    @payment_type_id INT = 2,           -- Default: Credit Card
    @credit_card_type_id INT = NULL,    -- Credit card type (VISA, MC, etc.)
    @authorization_code NVARCHAR(50) = NULL,
    @reference_number NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @closed_by NVARCHAR(100);
        DECLARE @bill_amt DECIMAL(18, 6);
        DECLARE @bill_disc_amt DECIMAL(18, 6);
        DECLARE @total_vat DECIMAL(18, 6);
        DECLARE @customer_key INT;
        DECLARE @trans_hdr_id NVARCHAR(50);

        -- Get the employee name for closing the bill
        SELECT TOP 1 @closed_by = name 
        FROM mst_employee 
        WHERE name = 'Self Checkout';
        
        -- Fallback to 'Online Order' if 'Self Checkout' employee doesn't exist
        IF @closed_by IS NULL
        BEGIN
            SELECT TOP 1 @closed_by = name 
            FROM mst_employee 
            WHERE name = 'Online Order';
        END

        -- Calculate bill amounts from bill details
        SELECT 
            @bill_amt = SUM(amount) + ISNULL(
                (SELECT SUM(ISNULL(tax_amount, 0)) 
                 FROM trn_bill_tax 
                 WHERE bill_hdr_key = @bill_hdr_key), 0),
            @bill_disc_amt = SUM(ISNULL(bill_wise_disc_amt, 0) + ISNULL(item_disc_amt, 0)),
            @total_vat = SUM(ISNULL(vat_amt, 0)) + ISNULL(
                (SELECT SUM(ISNULL(vat_amt, 0)) 
                 FROM trn_bill_tax 
                 WHERE bill_hdr_key = @bill_hdr_key), 0)
        FROM trn_bill_details
        WHERE bill_hdr_key = @bill_hdr_key;

        -- Get customer key and trans_hdr_id from bill header
        SELECT 
            @customer_key = customer_key,
            @trans_hdr_id = trans_hdr_id
        FROM trn_bill_header
        WHERE bill_hdr_key = @bill_hdr_key;

        -- Update the bill header with payment completion details
        UPDATE trn_bill_header 
        SET 
            bill_amt = @bill_amt,
            bill_disc_amt = @bill_disc_amt,
            bill_paid_amt = @bill_amt,
            total_vat = @total_vat,
            closed_by = @closed_by,
            bill_end_datetime = GETDATE()
        WHERE 
            bill_hdr_key = @bill_hdr_key;

        -- Insert payment record
        INSERT INTO trn_bill_payments (
            bill_hdr_key,
            customer_key,
            trans_hdr_id,
            payment_type_id,
            credit_card_type_id,
            payment_amt,
            auth_code,
            reference_no
        )
        VALUES (
            @bill_hdr_key,
            @customer_key,
            @trans_hdr_id,
            @payment_type_id,
            @credit_card_type_id,
            @bill_amt,
            @authorization_code,
            @reference_number
        );

        -- Update discount reason for items with discounts
        UPDATE trn_bill_details
        SET disc_reason_key = (
            SELECT TOP 1 reason_key 
            FROM mst_void_refund_discount_reasons 
            WHERE reason_type LIKE '%Discount%'
        )
        WHERE bill_hdr_key = @bill_hdr_key
          AND (
              ISNULL(item_disc_amt, 0) <> 0 
              OR ISNULL(bill_wise_disc_amt, 0) <> 0
          );

        -- Return success result with bill details
        -- Column names must match C# PaymentCompletionResult model properties
        SELECT 
            @bill_hdr_key AS BillHdrKey,
            @bill_amt AS BillAmt,
            @bill_disc_amt AS BillDiscAmt,
            @total_vat AS TotalVat,
            'SUCCESS' AS Status,
            'Payment completed successfully' AS Message;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error result with matching column names
        SELECT 
            @bill_hdr_key AS BillHdrKey,
            NULL AS BillAmt,
            NULL AS BillDiscAmt,
            NULL AS TotalVat,
            'ERROR' AS Status,
            ERROR_MESSAGE() AS Message;
            
        -- Re-raise the error
        DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
        SELECT 
            @ErrorMessage = ERROR_MESSAGE(), 
            @ErrorSeverity = ERROR_SEVERITY(), 
            @ErrorState = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- Grant execute permission (adjust as needed for your security model)
-- GRANT EXECUTE ON [dbo].[p_complete_selfcheckout_payment] TO [YourAppUser];
-- GO
