using System;
using System.Collections.Generic;
using Bookstore.Reporting.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Reporting;

public partial class ReportingContext : DbContext
{
    public ReportingContext(DbContextOptions<ReportingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BookSale> BookSales { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ImportLog> ImportLogs { get; set; }

    public virtual DbSet<VwDailySale> VwDailySales { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookSale>(entity =>
        {
            entity.HasKey(e => e.SaleId);

            entity.ToTable("BOOK_SALES", "reporting");

            entity.HasIndex(e => e.SoldAt, "IX_BOOK_SALES_SOLD_AT");

            entity.Property(e => e.SaleId).HasColumnName("SALE_ID");
            entity.Property(e => e.BookTitle)
                .HasMaxLength(200)
                .HasColumnName("BOOK_TITLE");
            entity.Property(e => e.CategoryId).HasColumnName("CATEGORY_ID");
            entity.Property(e => e.Isbn)
                .HasMaxLength(13)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ISBN");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("([QUANTITY]*[UNIT_PRICE])", true)
                .HasColumnType("decimal(21, 2)")
                .HasColumnName("LINE_TOTAL");
            entity.Property(e => e.Quantity).HasColumnName("QUANTITY");
            entity.Property(e => e.RowVer)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("ROW_VER");
            entity.Property(e => e.SoldAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_BOOK_SALES_SOLD_AT")
                .HasColumnName("SOLD_AT");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("UNIT_PRICE");

            entity.HasOne(d => d.Category).WithMany(p => p.BookSales)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOK_SALES_CATEGORY");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("CATEGORY", "reporting");

            entity.Property(e => e.CategoryId).HasColumnName("CATEGORY_ID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("CATEGORY_NAME");
        });

        modelBuilder.Entity<ImportLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("IMPORT_LOG", "reporting");

            entity.Property(e => e.FileName)
                .HasMaxLength(260)
                .HasColumnName("FILE_NAME");
            entity.Property(e => e.ImportedAt)
                .HasPrecision(0)
                .HasColumnName("IMPORTED_AT");
            entity.Property(e => e.RowCount).HasColumnName("ROW_COUNT");
        });

        modelBuilder.Entity<VwDailySale>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DAILY_SALES", "reporting");

            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("CATEGORY_NAME");
            entity.Property(e => e.Revenue)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("REVENUE");
            entity.Property(e => e.SaleDate).HasColumnName("SALE_DATE");
            entity.Property(e => e.Units).HasColumnName("UNITS");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
