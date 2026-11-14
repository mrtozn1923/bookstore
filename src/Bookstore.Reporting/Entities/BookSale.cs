using System;
using System.Collections.Generic;

namespace Bookstore.Reporting.Entities;

/// <summary>
/// Read model for reporting.BOOK_SALES (owned by the DBA team). Generated code: put changes in a partial class.
/// </summary>
public partial class BookSale
{
    public long SaleId { get; init; }

    public string Isbn { get; init; } = null!;

    public string BookTitle { get; init; } = null!;

    public int CategoryId { get; init; }

    public DateTime SoldAt { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal? LineTotal { get; init; }

    public byte[] RowVer { get; init; } = null!;

    public Category Category { get; init; } = null!;
}
