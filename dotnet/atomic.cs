using System.Text.RegularExpressions;

// \d+ gives back the last digit so that 0 can match; the atomic group does not give anything back
Console.WriteLine(Regex.IsMatch("1230", @"^\d+0$"));
Console.WriteLine(Regex.IsMatch("1230", @"^(?>\d+)0$"));

// .NET has atomic groups but no possessive quantifiers
try
{
    Regex.IsMatch("1230", @"^\d++0$");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}
