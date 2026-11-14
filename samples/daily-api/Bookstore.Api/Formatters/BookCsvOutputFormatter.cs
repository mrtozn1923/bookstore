using System.Globalization;
using System.Text;
using Bookstore.Api.Catalog;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Bookstore.Api.Formatters;

public class BookCsvOutputFormatter : TextOutputFormatter
{
    public BookCsvOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
        SupportedEncodings.Add(Encoding.UTF8);
    }

    // Yalnız kitap listelerini yazabiliriz; diğer tipler için sıradaki formatter'a geçilir
    protected override bool CanWriteType(Type? type) =>
        typeof(IEnumerable<Book>).IsAssignableFrom(type);

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        StringBuilder csv = new();
        csv.AppendLine("isbn,title,price,category");
        foreach (Book book in (IEnumerable<Book>)context.Object!)
        {
            csv.AppendLine(string.Join(',',
                book.Isbn,
                Escape(book.Title),
                book.Price.ToString(CultureInfo.InvariantCulture),
                book.Category));
        }

        context.HttpContext.Response.Headers.ContentDisposition = "attachment; filename=books.csv";
        await context.HttpContext.Response.WriteAsync(csv.ToString(), selectedEncoding);
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
