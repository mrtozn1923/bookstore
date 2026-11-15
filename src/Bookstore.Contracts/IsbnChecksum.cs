namespace Bookstore.Contracts;

internal static class IsbnChecksum
{
    // ISBN-13: weights alternate 1 and 3; the check digit completes the sum to a multiple of 10.
    internal static int Compute(ReadOnlySpan<char> firstTwelveDigits)
    {
        int sum = 0;
        for (int i = 0; i < firstTwelveDigits.Length; i++)
        {
            sum += (firstTwelveDigits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return (10 - sum % 10) % 10;
    }
}
