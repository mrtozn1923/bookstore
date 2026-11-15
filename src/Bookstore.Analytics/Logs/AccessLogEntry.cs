namespace Bookstore.Analytics.Logs;

public sealed record AccessLogEntry(
    DateTimeOffset Time,
    string Method,
    string Path,
    int Status,
    string? Referer,
    string? UserAgent);
