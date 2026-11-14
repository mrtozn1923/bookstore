namespace Bookstore.OrderImport.Worker;

public sealed class OrderImportOptions
{
    public string InboxPath { get; set; } = "inbox";
    public string ProcessedPath { get; set; } = "processed";
    public string Filter { get; set; } = "orders-*.jsonl";
    public TimeSpan QuietPeriod { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan RescanInterval { get; set; } = TimeSpan.FromSeconds(30);
}
