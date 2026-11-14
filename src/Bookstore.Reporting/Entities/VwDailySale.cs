using System;
using System.Collections.Generic;

namespace Bookstore.Reporting.Entities;

/// <summary>
/// Read model for reporting.vw_DAILY_SALES (owned by the DBA team). Generated code: put changes in a partial class.
/// </summary>
public partial class VwDailySale
{
    public DateOnly? SaleDate { get; init; }

    public string CategoryName { get; init; } = null!;

    public int? Units { get; init; }

    public decimal? Revenue { get; init; }
}
