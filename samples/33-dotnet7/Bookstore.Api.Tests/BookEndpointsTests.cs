using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Bookstore.Api.Tests;

public class BookEndpointsTests
{
    [Fact]
    public void GetById_UnknownId_ReturnsNotFound()
    {
        var result = BookEndpoints.GetById(99, new InMemoryBookService());

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public void CreateBookRequest_MissingTitle_FailsToDeserialize()
    {
        // C# 11 raw string literal: write the JSON without escape characters
        var json = """
            { "price": 10 }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateBookRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public void GetById_KnownId_ReturnsBook()
    {
        var result = BookEndpoints.GetById(1, new InMemoryBookService());

        var ok = Assert.IsType<Ok<Book>>(result.Result);
        Assert.Equal("Dune", ok.Value?.Title);
    }
}
