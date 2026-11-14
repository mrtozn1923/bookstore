using System.Diagnostics;

// .NET 10: an ActivitySource (and a Meter) can say which semantic conventions version its telemetry follows.
public static class BookstoreTelemetry
{
    public static readonly ActivitySource Catalog = new(new ActivitySourceOptions("Bookstore.Catalog")
    {
        Version = "1.0.0",
        TelemetrySchemaUrl = "https://opentelemetry.io/schemas/1.37.0"
    });
}
