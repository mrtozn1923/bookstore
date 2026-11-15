#:property InvariantGlobalization=true
using System.Globalization;

try
{
    CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
    Console.WriteLine(decimal.Parse("1.149,90", NumberStyles.Number, tr));
}
catch (Exception ex)
{
    Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
}
