using System;
using System.Collections.Generic;

namespace Bookstore.Reporting.Entities;

/// <summary>
/// Read model for reporting.IMPORT_LOG (owned by the DBA team). Generated code: put changes in a partial class.
/// </summary>
public partial class ImportLog
{
    public string FileName { get; init; } = null!;

    public DateTime ImportedAt { get; init; }

    public int RowCount { get; init; }
}
