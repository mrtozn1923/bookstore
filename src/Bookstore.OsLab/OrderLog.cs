namespace Bookstore.OsLab;

public static class OrderLog
{
    public static string FormatLine(int orderNo) =>
        $"order={orderNo:D6} isbn=9780441172719 qty=1 total=420.00 TRY";

    // Her satır için dosya açılır, yazılır ve kapatılır
    public static void WriteNaive(string path, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            File.AppendAllText(path, FormatLine(i) + "\n");
        }
    }

    // Dosya bir kez açılır; satırlar StreamWriter'ın buffer'ında birikir
    public static void WriteBuffered(string path, int count)
    {
        using StreamWriter writer = new(path, append: true);
        for (int i = 1; i <= count; i++)
        {
            writer.Write(FormatLine(i));
            writer.Write('\n');
        }
    }
}
