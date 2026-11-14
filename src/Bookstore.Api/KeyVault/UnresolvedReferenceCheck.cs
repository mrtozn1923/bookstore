namespace Bookstore.Api.KeyVault;

public static class UnresolvedReferenceCheck
{
    private const string ReferencePrefix = "@Microsoft.KeyVault(";

    // App Service çözemediği reference'ı değer olarak bırakır; bunu açılışta yakala
    public static void ThrowIfAny(IConfiguration configuration)
    {
        var unresolved = configuration.AsEnumerable()
            .Where(pair => pair.Value?.StartsWith(ReferencePrefix, StringComparison.OrdinalIgnoreCase) == true)
            .Select(pair => pair.Key)
            .Order()
            .ToList();

        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unresolved Key Vault references: {string.Join(", ", unresolved)}");
        }
    }
}
