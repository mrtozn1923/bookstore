using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.ModuleName.Tests;

public sealed class ModuleNameModuleTests
{
    [Fact]
    public void AddModuleNameModule_returns_the_same_collection()
    {
        ServiceCollection services = new();

        Assert.Same(services, services.AddModuleNameModule());
    }
}
