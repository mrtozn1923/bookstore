#:property PublishAot=false
using System.Text.Encodings.Web;
using System.Text.Json;

var book = new { Title = "Çalıkuşu", Note = "The \"Hobbit\"\tdraft" };

Console.WriteLine(JsonSerializer.Serialize(book));

// non-ASCII letters are written as-is; HTML-sensitive characters stay escaped
JsonSerializerOptions relaxed = new() { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) };
Console.WriteLine(JsonSerializer.Serialize(book, relaxed));
