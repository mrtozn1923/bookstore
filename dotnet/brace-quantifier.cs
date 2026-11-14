using System.Text.RegularExpressions;

// is {,3} "at most three" or four literal characters?
Console.WriteLine($"{Regex.IsMatch("xx", "^x{,3}$")} {Regex.IsMatch("x{,3}", "^x{,3}$")}");
