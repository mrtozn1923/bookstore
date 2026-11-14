namespace Bookstore.MinimalApi.Endpoints;

public static class CategoryEndpoints
{
    private static readonly List<string> Categories = new() { "science-fiction", "classics", "fantasy" };

    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", GetAll);
        app.MapGet("/api/categories/{name}", GetByName);
        return app;
    }

    private static IResult GetAll()
    {
        return Results.Ok(Categories);
    }

    private static IResult GetByName(string name)
    {
        string? category = Categories.FirstOrDefault(c => c == name);
        return category is null ? Results.NotFound() : Results.Ok(category);
    }
}
