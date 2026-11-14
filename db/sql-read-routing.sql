CREATE TABLE dbo.PageViews (
    PageViewId bigint IDENTITY NOT NULL CONSTRAINT PK_PageViews PRIMARY KEY,
    Page       varchar(50) NOT NULL,
    ViewedAt   datetime2(0) NOT NULL CONSTRAINT DF_PageViews_ViewedAt DEFAULT SYSUTCDATETIME()
);
