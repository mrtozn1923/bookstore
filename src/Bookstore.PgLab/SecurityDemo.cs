using Npgsql;

namespace Bookstore.PgLab;

public static class SecurityDemo
{
    // Opens one connection per SSL Mode and reports what the server saw (pg_stat_ssl)
    public static async Task TlsAsync(string connectionString, string? rootCertificate)
    {
        List<(string Label, Action<NpgsqlConnectionStringBuilder> Configure)> attempts =
        [
            ("Disable", b => b.SslMode = SslMode.Disable),
            ("Prefer (default)", b => { }),
            ("Require", b => b.SslMode = SslMode.Require),
            ("VerifyFull", b => b.SslMode = SslMode.VerifyFull),
        ];
        if (rootCertificate is not null)
        {
            attempts.Add(("VerifyFull + Root Certificate", b =>
            {
                b.SslMode = SslMode.VerifyFull;
                b.RootCertificate = rootCertificate;
            }));
        }

        foreach ((string label, Action<NpgsqlConnectionStringBuilder> configure) in attempts)
        {
            NpgsqlConnectionStringBuilder builder = new(connectionString) { Pooling = false };
            configure(builder);
            try
            {
                await using NpgsqlConnection connection = new(builder.ConnectionString);
                await connection.OpenAsync();
                await using NpgsqlCommand command = new(
                    "SELECT ssl, coalesce(version, '-'), coalesce(cipher, '-') FROM pg_stat_ssl WHERE pid = pg_backend_pid()",
                    connection);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                Console.WriteLine($"{label,-30} ssl={reader.GetBoolean(0)} {reader.GetString(1)} {reader.GetString(2)}");
            }
            catch (Exception ex) when (ex is NpgsqlException or PostgresException or System.Security.Authentication.AuthenticationException)
            {
                Exception inner = ex.InnerException ?? ex;
                Console.WriteLine($"{label,-30} {ex.GetType().Name}: {FirstLine(ex.Message)}");
                if (!ReferenceEquals(inner, ex))
                {
                    Console.WriteLine($"{"",-30} -> {inner.GetType().Name}: {FirstLine(inner.Message)}");
                }
            }
        }
    }

    private static string FirstLine(string text) => text.Split('\n')[0].Trim();
}
