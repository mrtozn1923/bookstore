using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class CompatDemo
{
    // Commands this series used (plus two Valkey-only ones), checked on both servers with COMMAND DOCS
    private static readonly string[] Commands =
    [
        "HEXPIRE", "HGETEX", "HSETEX", "HGETDEL", "VADD", "JSON.SET", "FT.SEARCH", "BF.ADD",
        "XACKDEL", "DELEX", "DIGEST", "MSETEX", "HOTKEYS", "XCFGSET", "INCREX", "XNACK", "ARSET",
        "HIMPORT", "LMOVEM", "SUNIONCARD", "SDIFFCARD", "BACKUP", "DELIFEQ", "COMMANDLOG"
    ];

    // Options added to existing commands: the only way to know is to run them
    private static readonly (string Name, string Command)[] Options =
    [
        ("SET ... IFEQ", "SET compat:k v IFEQ x"),
        ("XADD ... IDMP", "XADD compat:s IDMP p i1 * f v"),
        ("ZUNIONSTORE ... AGGREGATE COUNT", "ZUNIONSTORE compat:z 1 compat:a AGGREGATE COUNT")
    ];

    public static async Task CompareAsync(string redisConnection, string valkeyConnection)
    {
        await using ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);
        await using ConnectionMultiplexer valkey = await ConnectionMultiplexer.ConnectAsync(valkeyConnection);
        IDatabase r = redis.GetDatabase();
        IDatabase v = valkey.GetDatabase();

        Console.WriteLine($"{"command",-32}{"Redis 8.10.3",-16}{"Valkey 9.1.3",-16}");
        foreach (string command in Commands)
        {
            Console.WriteLine($"{command,-32}{await SinceAsync(r, command),-16}{await SinceAsync(v, command),-16}");
        }
        foreach ((string name, string command) in Options)
        {
            Console.WriteLine($"{name,-32}{await TryAsync(r, command),-16}{await TryAsync(v, command),-16}");
        }
        await r.KeyDeleteAsync(["compat:k", "compat:s", "compat:z"]);
        await v.KeyDeleteAsync(["compat:k", "compat:s", "compat:z"]);
    }

    // COMMAND DOCS name -> "since" of the command, or "-" when the server does not know it
    private static async Task<string> SinceAsync(IDatabase db, string command)
    {
        RedisResult[] docs = (RedisResult[])(await db.ExecuteAsync("COMMAND", "DOCS", command))!;
        if (docs.Length == 0)
        {
            return "-";
        }
        RedisResult[] fields = (RedisResult[])docs[1]!;
        for (int i = 0; i < fields.Length - 1; i += 2)
        {
            if ((string?)fields[i] == "since")
            {
                return (string)fields[i + 1]!;
            }
        }
        return "yes";
    }

    private static async Task<string> TryAsync(IDatabase db, string command)
    {
        string[] parts = command.Split(' ');
        try
        {
            await db.ExecuteAsync(parts[0], [.. parts[1..]]);
            return "ok";
        }
        catch (RedisServerException ex)
        {
            return ex.Message.StartsWith("ERR syntax") ? "syntax error" : ex.Message.Split(' ')[0] + " " + ex.Message.Split(' ').ElementAtOrDefault(1);
        }
    }
}
