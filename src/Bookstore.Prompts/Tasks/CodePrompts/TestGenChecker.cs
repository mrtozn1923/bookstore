using System.Globalization;

namespace Bookstore.Prompts.Tasks.CodePrompts;

public sealed record TestGenReport(int Cases, List<string> WrongExpected, List<int> MissingBoundaries);

// Every generated case is run against the real method; boundaries come from the specification
public static class TestGenChecker
{
    public static readonly int[] Boundaries = [0, 1, 19, 20, 5000, 5001];

    public static string Actual(int quantity) => Actual(InstitutionalDiscount.RateFor, quantity);

    public static string Actual(Func<int, decimal> rateFor, int quantity)
    {
        try
        {
            return rateFor(quantity) == 0m ? "0" : "0.10";
        }
        catch (ArgumentOutOfRangeException)
        {
            return "throws";
        }
    }

    public static string Normalize(string expected)
    {
        string text = expected.Trim().TrimEnd('%').ToLowerInvariant();
        if (text.Contains("throw") || text.Contains("exception"))
        {
            return "throws";
        }
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            decimal rate = expected.Contains('%') ? value / 100 : value;
            return rate == 0m ? "0" : rate == 0.10m ? "0.10" : text;
        }
        return text;
    }

    public static TestGenReport Check(GeneratedTests tests) => Check(tests, InstitutionalDiscount.RateFor);

    // The same cases can be run against any version of the method, for example the one in a pull request
    public static TestGenReport Check(GeneratedTests tests, Func<int, decimal> rateFor)
    {
        List<string> wrong =
        [
            .. tests.Cases
                .Where(c => Normalize(c.Expected) != Actual(rateFor, c.Quantity))
                .Select(c => $"{c.Quantity}: expected {c.Expected}, method returns {Actual(rateFor, c.Quantity)}"),
        ];
        List<int> missing = [.. Boundaries.Where(b => !tests.Cases.Any(c => c.Quantity == b))];
        return new(tests.Cases.Count, wrong, missing);
    }
}
