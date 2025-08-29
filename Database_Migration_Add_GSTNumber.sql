-- Database Migration Script: Add GST Number Column to Vendors Table
-- This script adds a new GST Number column to the Vendors table

-- Add GST Number column to Vendors table
ALTER TABLE Vendors 
ADD GSTNumber NVARCHAR(15) NULL;

-- Add comment to document the column
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'GST Number for the vendor (optional field)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Vendors', 
    @level2type = N'COLUMN', @level2name = N'GSTNumber';

-- Update existing stored procedures if they exist
-- Note: You may need to update your stored procedures to include the new column

PRINT 'GST Number column added successfully to Vendors table.';
PRINT 'Please update any stored procedures that insert or update vendor data to include the new GSTNumber column.';
