-- =====================================================
-- Migration: AddVnpayAndPassengerFields
-- Date: 2026-03-15
-- =====================================================

-- 1. Add passenger info to BookingDetail
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'BookingDetail' AND COLUMN_NAME = 'PassengerName')
BEGIN
    ALTER TABLE BookingDetail
        ADD PassengerName NVARCHAR(100) NULL,
            PassengerPhone NVARCHAR(20)  NULL;
END

-- 2. Add VNPay tracking fields to Payment
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Payment' AND COLUMN_NAME = 'VnpayTransactionId')
BEGIN
    ALTER TABLE Payment
        ADD VnpayTransactionId NVARCHAR(100) NULL,
            VnpayOrderInfo     NVARCHAR(255) NULL,
            Status             NVARCHAR(50)  NULL;   -- 'Pending', 'Paid', 'Failed'
END
