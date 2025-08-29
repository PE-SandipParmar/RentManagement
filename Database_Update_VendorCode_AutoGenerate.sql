-- Database Update Script: Auto-Generate Vendor Codes
-- This script updates the Vendors table to support auto-generated vendor codes

-- Add a unique constraint on VendorCode to ensure uniqueness
-- First, let's check if there are any duplicate vendor codes
SELECT VendorCode, COUNT(*) as Count
FROM Vendors 
WHERE IsActiveRecord = 1 
GROUP BY VendorCode 
HAVING COUNT(*) > 1;

-- If there are duplicates, you'll need to resolve them first
-- Then add the unique constraint

-- Add unique constraint on VendorCode (only for active records)
-- Note: This will fail if there are duplicate vendor codes
-- You may need to clean up duplicates first

-- Create a function to generate the next vendor code
CREATE OR ALTER FUNCTION GetNextVendorCode()
RETURNS NVARCHAR(10)
AS
BEGIN
    DECLARE @NextCode NVARCHAR(10);
    DECLARE @LastNumber INT;
    
    -- Get the highest vendor code number
    SELECT TOP 1 @LastNumber = CAST(SUBSTRING(VendorCode, 4, LEN(VendorCode) - 3) AS INT)
    FROM Vendors 
    WHERE VendorCode LIKE 'VEN%' 
    AND IsActiveRecord = 1 
    ORDER BY CAST(SUBSTRING(VendorCode, 4, LEN(VendorCode) - 3) AS INT) DESC;
    
    -- If no existing codes, start with 1, otherwise increment
    IF @LastNumber IS NULL
        SET @NextCode = 'VEN001';
    ELSE
        SET @NextCode = 'VEN' + RIGHT('000' + CAST(@LastNumber + 1 AS VARCHAR(10)), 3);
    
    RETURN @NextCode;
END
GO

-- Add comment to document the function
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Generates the next sequential vendor code in format VEN001, VEN002, etc.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'FUNCTION', @level1name = N'GetNextVendorCode';

-- Test the function
SELECT dbo.GetNextVendorCode() as NextVendorCode;

-- Update existing vendor codes if they don't follow the VEN### pattern
-- This is optional - only run if you want to standardize existing codes
/*
UPDATE Vendors 
SET VendorCode = 'VEN' + RIGHT('000' + CAST(ROW_NUMBER() OVER (ORDER BY CreatedDate) AS VARCHAR(10)), 3)
WHERE VendorCode NOT LIKE 'VEN%' 
AND IsActiveRecord = 1;
*/

PRINT 'Vendor code auto-generation setup completed successfully.';
PRINT 'The system will now generate vendor codes in the format VEN001, VEN002, etc.';
PRINT 'Make sure to test the GetNextVendorCode() function before using it in production.';
