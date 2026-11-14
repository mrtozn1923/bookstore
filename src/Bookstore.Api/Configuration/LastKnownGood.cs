using Microsoft.Extensions.Options;

namespace Bookstore.Configuration;

// Reads options that can change at runtime and keeps the last valid value when a change does not bind or validate.
// Without it, IOptionsMonitor.CurrentValue throws on every call until someone fixes the file.
public sealed class LastKnownGood<T>(IOptionsMonitor<T> monitor, ILogger<LastKnownGood<T>> logger) where T : class
{
    private T current = monitor.CurrentValue;   // validated at startup (ValidateOnStart)
    private string? lastError;

    public T Value
    {
        get
        {
            try
            {
                current = monitor.CurrentValue;
                lastError = null;
            }
            catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException)
            {
                // Log and count once per distinct problem, not on every request.
                if (ex.Message != lastError)
                {
                    lastError = ex.Message;
                    SwitchesFile.Rejected.Add(1, new KeyValuePair<string, object?>("reason", "invalid"));
                    logger.LogError(ex, "Rejected new {Options} values; keeping the last valid ones", typeof(T).Name);
                }
            }
            return current;
        }
    }
}
