/* Post 04: six tables with the same institutional order row, different key strategies.
   Run against an empty database, then: dotnet run --project samples/Cd04.PublicId -- bench
   then sql/cd04-measure.sql */
CREATE TABLE dbo.OrdInt    (Id int IDENTITY CONSTRAINT PK_OrdInt PRIMARY KEY,
                            CreatedAt datetime2 NOT NULL, TotalAmount decimal(19, 4) NOT NULL, Note nvarchar(100) NOT NULL);
CREATE TABLE dbo.OrdGuidV4 (Id uniqueidentifier CONSTRAINT PK_OrdGuidV4 PRIMARY KEY,   /* Guid.NewGuid() */
                            CreatedAt datetime2 NOT NULL, TotalAmount decimal(19, 4) NOT NULL, Note nvarchar(100) NOT NULL);
CREATE TABLE dbo.OrdGuidV7 (Id uniqueidentifier CONSTRAINT PK_OrdGuidV7 PRIMARY KEY,   /* Guid.CreateVersion7() */
                            CreatedAt datetime2 NOT NULL, TotalAmount decimal(19, 4) NOT NULL, Note nvarchar(100) NOT NULL);
CREATE TABLE dbo.OrdGuidEf (Id uniqueidentifier CONSTRAINT PK_OrdGuidEf PRIMARY KEY,   /* SequentialGuidValueGenerator */
                            CreatedAt datetime2 NOT NULL, TotalAmount decimal(19, 4) NOT NULL, Note nvarchar(100) NOT NULL);
CREATE TABLE dbo.OrdNewSeq (Id uniqueidentifier CONSTRAINT PK_OrdNewSeq PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                            CreatedAt datetime2 NOT NULL, TotalAmount decimal(19, 4) NOT NULL, Note nvarchar(100) NOT NULL);
CREATE TABLE dbo.OrdSplit  (Id bigint IDENTITY CONSTRAINT PK_OrdSplit PRIMARY KEY, PublicId uniqueidentifier NOT NULL,
                            CreatedAt datetime2 NOT NULL, TotalAmount decimal(19, 4) NOT NULL, Note nvarchar(100) NOT NULL);
CREATE UNIQUE INDEX UX_OrdSplit_PublicId ON dbo.OrdSplit (PublicId);   /* Guid.CreateVersion7() */
