using Aspire.Hosting.JavaScript;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Deployment target: Docker Compose by default, Kubernetes with "-- --DeployTarget=k8s"
if (builder.Configuration["DeployTarget"] == "k8s")
{
    // The cluster pulls images from a registry; its address is a deploy-time parameter (APIs in preview)
#pragma warning disable ASPIRECOMPUTE003
    IResourceBuilder<ContainerRegistryResource> registry = builder.AddContainerRegistry("registry", builder.AddParameter("registry-endpoint"));
    builder.AddKubernetesEnvironment("k8s").WithContainerRegistry(registry);
#pragma warning restore ASPIRECOMPUTE003
}
else
{
    builder.AddDockerComposeEnvironment("compose");
}

IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-password", secret: true);

IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql", sqlPassword)
    .WithImageTag("2025-latest")
    .WithContainerName("aspire-sql")
    .WithDataVolume("aspire-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> bookstoreDb = sql.AddDatabase("bookstoredb", "BookstoreDb");

IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithContainerName("aspire-redis")
    .WithClearCacheCommand();

IResourceBuilder<RabbitMQServerResource> messaging = builder.AddRabbitMQ("messaging")
    .WithContainerName("aspire-rabbitmq")
    .WithManagementPlugin();

IResourceBuilder<ProjectResource> migrations = builder.AddProject<Projects.Bookstore_MigrationService>("migrations")
    .WithReference(bookstoreDb)
    .WaitFor(bookstoreDb);

// Redis and Keycloak are references but not WaitFor: the catalog reads without them
IResourceBuilder<ProjectResource> catalog = builder.AddProject<Projects.Bookstore_Api>("catalog")
    .WithReference(bookstoreDb)
    .WithReference(redis)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

if (builder.ExecutionContext.IsRunMode)
{
    // 200,000 generated books are for local performance work only
    migrations.WithEnvironment("Seed__GeneratedBooks", "200000");

    IResourceBuilder<ParameterResource> backofficeSecret = builder.AddParameter("backoffice-client-secret", secret: true);

    // Realm import is for local development only; it is not part of publish/deploy
    IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8080)
        .WithContainerName("aspire-keycloak")
        .WithRealmImport("./Realms")
        .WithEnvironment("BOOKSTORE_BACKOFFICE_SECRET", backofficeSecret);
    catalog.WithReference(keycloak);

    // Catches order e-mails locally; started from the dashboard only when needed
    builder.AddMailpit("mailpit")
        .WithContainerName("aspire-mailpit")
        .WithExplicitStart();
}
else
{
    // Deployed environments use an existing Keycloak; its https Authority is a deploy-time parameter
    catalog.WithEnvironment("Keycloak__Authority", builder.AddParameter("keycloak-authority"));
}

IResourceBuilder<ProjectResource> ordering = builder.AddProject<Projects.Bookstore_Ordering_Api>("ordering")
    .WithReference(catalog)
    .WithReference(messaging)
    .WaitFor(catalog)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookstore_Inventory>("inventory")
    .WithReference(messaging)
    .WaitFor(messaging);

IResourceBuilder<ProjectResource> gateway = builder.AddProject<Projects.Bookstore_Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

IResourceBuilder<JavaScriptAppResource> web = builder.AddJavaScriptApp("web", "../../web/bookstore-web", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)
    .WaitFor(gateway);

// In publish the gateway image carries Angular's build output and serves it
gateway.PublishWithContainerFiles(web, "./wwwroot");

builder.Build().Run();
