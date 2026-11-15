using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.IntegrationTests.Infrastructure;

public static class HttpAssertions
{
    public static async Task<HttpValidationProblemDetails> AssertValidationProblemAsync(this HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        HttpValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        return Assert.IsType<HttpValidationProblemDetails>(problem);
    }

    public static async Task<ProblemDetails> AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return Assert.IsType<ProblemDetails>(problem);
    }

    public static int AssertCreatedAt(this HttpResponseMessage response, string pathPrefix)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        string location = Assert.IsType<Uri>(response.Headers.Location).OriginalString;
        Assert.StartsWith(pathPrefix, location);
        return int.Parse(location[pathPrefix.Length..]);
    }
}
