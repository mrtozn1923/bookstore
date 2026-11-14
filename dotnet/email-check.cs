using System.Net.Mail;
using System.Text.RegularExpressions;

// a typical "email regex" next to MailAddress.TryCreate
Regex emailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

string[] addresses =
[
    "ada@example.com",
    "ada.lovelace+books@example.com",
    "ada@example",
    "\"Ada Lovelace\"@example.com",
    "Ada Lovelace <ada@example.com>",
    "ada@example..com",
    "ada@@example.com",
];

Console.WriteLine($"{"address",-34} {"regex",-6} MailAddress");
foreach (string address in addresses)
{
    bool parsed = MailAddress.TryCreate(address, out MailAddress? mail);
    string result = parsed ? $"True  -> {mail!.Address}" : "False";
    Console.WriteLine($"{address,-34} {emailRegex.IsMatch(address),-6} {result}");
}
