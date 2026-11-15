using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Jobs;
using Bookstore.Notifications.Samples;

namespace Bookstore.Api.Jobs;

// The series has no Ordering database; the real sources read the Ordering module's tables
public sealed class SampleInvoiceSource : IInvoiceSource
{
    public Task<InvoiceModel> GetAsync(int orderId, CancellationToken cancellationToken) => Task.FromResult(BookstoreSampleData.Invoice(orderId));
}

public sealed class SampleSalesReportSource : ISalesReportSource
{
    public Task<MonthlySalesReport> GetAsync(DateOnly month, CancellationToken cancellationToken) =>
        Task.FromResult(BookstoreSampleData.MonthlySales(month, rowCount: 2_000));
}
