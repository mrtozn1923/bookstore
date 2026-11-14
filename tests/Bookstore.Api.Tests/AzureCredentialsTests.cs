using Azure.Identity;
using Bookstore.Api.Cloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Bookstore.Api.Tests;

public class AzureCredentialsTests
{
    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Bookstore.Api";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Production_uses_managed_identity()
    {
        var credential = AzureCredentials.Create(new TestEnvironment("Production"), Config());

        Assert.IsType<ManagedIdentityCredential>(credential);
    }

    [Fact]
    public void Aks_workload_identity_is_detected_from_the_federated_token_file()
    {
        var credential = AzureCredentials.Create(
            new TestEnvironment("Production"),
            Config(("AZURE_FEDERATED_TOKEN_FILE", "/var/run/secrets/azure/tokens/azure-identity-token")));

        Assert.IsType<WorkloadIdentityCredential>(credential);
    }

    [Fact]
    public void Development_never_uses_managed_identity()
    {
        var credential = AzureCredentials.Create(new TestEnvironment("Development"), Config());

        Assert.IsType<ChainedTokenCredential>(credential);
    }
}
