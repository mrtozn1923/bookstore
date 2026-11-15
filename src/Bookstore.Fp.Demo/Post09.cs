using System.Text.RegularExpressions;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Fp.Demo;

public static class Post09
{
    public static void Trace()
    {
        // A rule with a bug, hidden in the middle of a composed pipeline
        Func<decimal, decimal> brokenRule = amount => amount / (amount > 200m ? 0m : 1m);
        Func<decimal, decimal> checkout = PriceRules.Combine([PriceRules.PercentOff(0.10m), brokenRule, PriceRules.AmountOff(15m), PriceRules.RoundToKurus]);

        try
        {
            Console.WriteLine(checkout(249.40m));
        }
        catch (DivideByZeroException ex)
        {
            // Strip file paths so the output is the same on every machine
            foreach (string frame in ex.StackTrace!.Split(Environment.NewLine))
                Console.WriteLine(Regex.Replace(frame, @" in .*$", ""));
        }
    }
}
