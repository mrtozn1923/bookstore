using System.Reflection;
using System.Runtime.InteropServices;

namespace Bookstore.Api;

public sealed record AppVersion(string Version, string? Commit, string AssemblyVersion, string FileVersion, string Runtime)
{
    public static AppVersion FromAssembly(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "unknown";

        // "2026.11.3+5114f85..." -> version and commit
        var parts = informational.Split('+', 2);
        return new(parts[0], parts.Length == 2 ? parts[1] : null, assemblyVersion, fileVersion, RuntimeInformation.FrameworkDescription);
    }
}
