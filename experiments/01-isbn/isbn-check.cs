#:property PublishAot=false
// Code written by qwen3:4b (unchanged), then checked against cases we wrote ourselves.
(string? Input, bool Expected)[] cases =
[
    ("9780261102217", true),        // The Hobbit, from the Bookstore seed
    ("978-0-261-10221-7", true),    // same ISBN with hyphens
    ("978 0 261 10221 7", true),    // same ISBN with spaces
    ("9780261102218", false),       // wrong check digit
    ("978026110221", false),        // 12 digits
    (null, false),
    ("ISBN 9780261102217", false),  // letters are not allowed by the spec
    ("9780261102217-X", false),     // trailing garbage
];

int failed = 0;
foreach ((string? input, bool expected) in cases)
{
    bool actual = Isbn13.IsValid(input);
    string mark = actual == expected ? "ok  " : "FAIL";
    if (actual != expected) failed++;
    Console.WriteLine($"{mark} {input ?? "null",-22} expected={expected,-5} actual={actual}");
}
Console.WriteLine($"{cases.Length - failed}/{cases.Length} passed");

public static class Isbn13
{
    public static bool IsValid(string? isbn)
    {
        if (isbn == null)
            return false;

        var cleaned = new string(isbn.Where(c => char.IsDigit(c)).ToArray());

        if (cleaned.Length != 13)
            return false;

        int total = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = cleaned[i] - '0';
            int weight = (i % 2 == 0) ? 1 : 3;
            total += digit * weight;
        }

        int checkDigit = (10 - (total % 10)) % 10;

        return cleaned[12] - '0' == checkDigit;
    }
}
