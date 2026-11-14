namespace Bookstore.AppHost.Tests;

// One AppHost per test class. Resources the scenario does not need stay stopped.
public abstract class BookstoreAppFixture(params string[] notNeeded) : IAsyncLifetime
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);

    // Generated per run: no user secrets needed on a CI machine
    public string BackofficeSecret { get; } = Guid.NewGuid().ToString("N");

    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        using CancellationTokenSource cts = new(StartTimeout);

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Bookstore_AppHost>(
        [
            $"Parameters:sql-password={Guid.NewGuid():N}Aa1!",
            $"Parameters:backoffice-client-secret={BackofficeSecret}"
        ], cts.Token);

        foreach (string name in notNeeded)
        {
            builder.CreateResourceBuilder<IResource>(name).WithExplicitStart();
        }

        // Tests must not touch the developer's database: own container names, session lifetime, no volumes
        foreach (ContainerResource container in builder.Resources.OfType<ContainerResource>().ToList())
        {
            builder.CreateResourceBuilder(container)
                .WithContainerName($"aspire-test-{container.Name}")
                .WithLifetime(ContainerLifetime.Session);
            foreach (ContainerMountAnnotation volume in container.Annotations.OfType<ContainerMountAnnotation>()
                         .Where(m => m.Type == ContainerMountType.Volume).ToList())
            {
                container.Annotations.Remove(volume);
            }
        }

        // A small catalog is enough for tests; seeding 200,000 books is not
        builder.CreateResourceBuilder<ProjectResource>("migrations").WithEnvironment("Seed__GeneratedBooks", "0");
        Configure(builder);

        App = await builder.BuildAsync(cts.Token);
        await App.StartAsync(cts.Token);
        await WaitUntilReadyAsync(App, cts.Token);
    }

    protected virtual void Configure(IDistributedApplicationTestingBuilder builder)
    {
    }

    protected abstract Task WaitUntilReadyAsync(DistributedApplication app, CancellationToken ct);

    public async Task DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}
