namespace Bookstore.Admin;

// Results go to stdout, problems to stderr; color is decoration only, never the message itself.
public static class ConsoleOutput
{
    public static void Success(string message) => Write(Console.Out, ConsoleColor.Green, message);

    public static void Warning(string message) => Write(Console.Error, ConsoleColor.Yellow, message);

    public static void Error(string message) => Write(Console.Error, ConsoleColor.Red, "error: " + message);

    private static void Write(TextWriter writer, ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        writer.WriteLine(message);
        Console.ResetColor();
    }
}
