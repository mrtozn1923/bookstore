using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.ModuleName;

public static class ModuleNameModule
{
    public static IServiceCollection AddModuleNameModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapModuleNameEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/modulename").WithTags("ModuleName");
        group.MapGet("/status", () => TypedResults.Ok("ModuleName module is running"));
        return app;
    }
}
