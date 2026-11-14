using System;
using System.Collections.Generic;

namespace Bookstore.Reporting.Entities;

/// <summary>
/// Read model for reporting.CATEGORY (owned by the DBA team). Generated code: put changes in a partial class.
/// </summary>
public partial class Category
{
    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = null!;

    public ICollection<BookSale> BookSales { get; init; } = new List<BookSale>();
}
