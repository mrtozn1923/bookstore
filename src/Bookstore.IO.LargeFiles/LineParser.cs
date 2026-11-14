namespace Bookstore.IO.LargeFiles;

// Parses one line without allocating; adds paid revenue to the day's total in cents.
public static class LineParser
{
    public const int Days = 400;
    private static readonly int FirstDayNumber = OrderLog.FirstDay.DayNumber;

    public static void Accumulate(ReadOnlySpan<byte> line, long[] centsPerDay)
    {
        int c1 = line.IndexOf((byte)',');
        int c2 = c1 + 1 + line[(c1 + 1)..].IndexOf((byte)',');
        int c3 = c2 + 1 + line[(c2 + 1)..].IndexOf((byte)',');
        ReadOnlySpan<byte> status = line[(c2 + 1)..c3];
        if (status.SequenceEqual("Pending"u8) || status.SequenceEqual("Cancelled"u8)) return;

        int year = Digits(line[..4]), month = Digits(line.Slice(5, 2)), day = Digits(line.Slice(8, 2));
        int dayIndex = new DateOnly(year, month, day).DayNumber - FirstDayNumber;
        centsPerDay[dayIndex] += ParseCents(line[(c3 + 1)..]);
    }

    // Processes every complete line in the chunk and returns how many bytes were consumed.
    public static int AccumulateLines(ReadOnlySpan<byte> chunk, long[] centsPerDay)
    {
        int consumed = 0;
        int newline;
        while ((newline = chunk[consumed..].IndexOf((byte)'\n')) >= 0)
        {
            Accumulate(chunk.Slice(consumed, newline), centsPerDay);
            consumed += newline + 1;
        }
        return consumed;
    }

    private static int Digits(ReadOnlySpan<byte> s)
    {
        int value = 0;
        foreach (byte b in s) value = value * 10 + (b - '0');
        return value;
    }

    private static long ParseCents(ReadOnlySpan<byte> s)
    {
        long value = 0;
        foreach (byte b in s)
        {
            if (b != '.') value = value * 10 + (b - '0');
        }
        return value;
    }
}
