-- Opening of post 07: UpdatedBy/UpdatedAt only remember the last change
CREATE TABLE dbo.InvoicesNaive
(
    Id            int IDENTITY PRIMARY KEY,
    InvoiceNumber varchar(16)   NOT NULL,
    TotalAmount   decimal(18,2) NOT NULL,
    DueDate       date          NOT NULL,
    UpdatedBy     nvarchar(100) NULL,
    UpdatedAt     datetime2(0)  NULL
);
INSERT dbo.InvoicesNaive (InvoiceNumber, TotalAmount, DueDate) VALUES ('INV-2026-0042', 48000, '2026-05-15');

UPDATE dbo.InvoicesNaive SET TotalAmount = 4800, UpdatedBy = N'alan', UpdatedAt = '2026-04-14 09:30' WHERE Id = 1;
UPDATE dbo.InvoicesNaive SET DueDate = '2026-05-30', UpdatedBy = N'batch-import', UpdatedAt = '2026-04-15 02:00' WHERE Id = 1;

SELECT InvoiceNumber, TotalAmount, DueDate, UpdatedBy, UpdatedAt FROM dbo.InvoicesNaive;
