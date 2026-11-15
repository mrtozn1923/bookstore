namespace Bookstore.Privacy.Protection;

// Format and checksum check for an 11-digit Turkish national ID number.
public static class NationalIdChecksum
{
    public static bool IsValid(string? value)
    {
        if (value is not { Length: 11 } || value[0] == '0' || !value.All(char.IsAsciiDigit))
        {
            return false;
        }

        int[] d = [.. value.Select(c => c - '0')];
        int odd = d[0] + d[2] + d[4] + d[6] + d[8];
        int even = d[1] + d[3] + d[5] + d[7];
        int tenth = ((odd * 7 - even) % 10 + 10) % 10;
        int eleventh = (d.Take(10).Sum()) % 10;
        return d[9] == tenth && d[10] == eleventh;
    }

    public static int ExpectedEleventhDigit(ReadOnlySpan<char> firstTen)
    {
        int sum = 0;
        foreach (char c in firstTen)
        {
            sum += c - '0';
        }
        return sum % 10;
    }

    public static int ExpectedTenthDigit(ReadOnlySpan<char> firstNine)
    {
        int odd = 0, even = 0;
        for (int i = 0; i < 9; i++)
        {
            int digit = firstNine[i] - '0';
            if (i % 2 == 0) odd += digit; else even += digit;
        }
        return ((odd * 7 - even) % 10 + 10) % 10;
    }
}
