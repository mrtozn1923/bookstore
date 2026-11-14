using System.Text.RegularExpressions;

string banner = "Dune: {discount}";
string discount = "$0.99 off";   // text typed by a store admin

Console.WriteLine(Regex.Replace(banner, @"\{discount\}", discount));
Console.WriteLine(Regex.Replace(banner, @"\{discount\}", discount.Replace("$", "$$")));
Console.WriteLine(Regex.Replace(banner, @"\{discount\}", _ => discount));
