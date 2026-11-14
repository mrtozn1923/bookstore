using System.Text.RegularExpressions;

string[] inputs = ["978-0-441-17271-9", "978-0-141-43958-7", "978-0-441-17271-8", "978-0441-17271-9", "978-0-441-17271-9\n", "977-0-441-17271-9"];

foreach (string input in inputs)
    Console.WriteLine($"{input.Replace("\n", "\\n"),-20} {IsIsbn13(input)}");

static bool IsIsbn13(string input)
{
    // format: 978/979 prefix, four hyphens, 17 characters in total
    if (!Regex.IsMatch(input, @"^97[89]-[0-9]{1,5}-[0-9]{1,7}-[0-9]{1,7}-[0-9]\z") || input.Length != 17)
        return false;

    // the check digit is not a regex job: weights 1 and 3, mod 10
    string digits = input.Replace("-", "");
    int sum = 0;
    for (int i = 0; i < 12; i++)
        sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
    return (10 - sum % 10) % 10 == digits[12] - '0';
}
