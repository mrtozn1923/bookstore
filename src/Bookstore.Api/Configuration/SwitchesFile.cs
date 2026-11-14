using System.Diagnostics.Metrics;

namespace Bookstore.Configuration;

// switches.json is shipped to every instance at once, so a broken file must neither stop instances from starting
// nor silently change what running instances do.
public static class SwitchesFile
{
    public const string MeterName = "Bookstore.Api";
    private static readonly Meter Meter = new(MeterName);
    internal static readonly Counter<long> Rejected =
        Meter.CreateCounter<long>("bookstore.switches.rejected", description: "switches.json loads or values that were rejected");

    private static volatile bool started;

    public static WebApplicationBuilder AddSwitchesFile(this WebApplicationBuilder builder)
    {
        string path = builder.Configuration["SwitchesFile"] ?? "switches.json";
        builder.Configuration.AddJsonFile(source =>
        {
            source.Path = path;
            source.Optional = true;
            source.ReloadOnChange = true;
            source.OnLoadException = context =>
            {
                Rejected.Add(1, new KeyValuePair<string, object?>("reason", "parse"));
                // While starting: go on with the appsettings values (fail open) instead of a crash loop.
                // While running: do not ignore it; the reload is dropped and the last values stay in effect.
                if (!started)
                {
                    context.Ignore = true;
                    Console.Error.WriteLine($"switches.json ignored at startup: {context.Exception.GetBaseException().Message}");
                }
            };
            source.ResolveFileProvider();
        });
        return builder;
    }

    public static void MarkStarted() => started = true;
}
