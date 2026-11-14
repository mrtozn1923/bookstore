IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-password", secret: true);

IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql", sqlPassword)
    .WithImageTag("2025-latest")
    .WithContainerName("aspire-sql")
    .WithDataVolume("aspire-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> bookstoreDb = sql.AddDatabase("bookstoredb", "BookstoreDb");

IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithContainerName("aspire-redis");

IResourceBuilder<ProjectResource> migrations = builder.AddProject<Projects.Bookstore_MigrationService>("migrations")
    .WithEnvironment("Seed__GeneratedBooks", "200000")
    .WithReference(bookstoreDb)
    .WaitFor(bookstoreDb);

IResourceBuilder<ProjectResource> catalog = builder.AddProject<Projects.Bookstore_Api>("catalog")
    .WithReference(bookstoreDb)
    .WithReference(redis)
    .WaitForCompletion(migrations)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

IResourceBuilder<ProjectResource> ordering = builder.AddProject<Projects.Bookstore_Ordering_Api>("ordering")
    .WithReference(catalog)
    .WaitFor(catalog)
    .WithHttpHealthCheck("/health");

IResourceBuilder<ProjectResource> gateway = builder.AddProject<Projects.Bookstore_Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WithHttpHealthCheck("/health");

// Catches order e-mails locally; started from the dashboard only when needed
builder.AddContainer("mailpit", "axllent/mailpit", "v1.33")
    .WithContainerName("aspire-mailpit")
    .WithHttpEndpoint(targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp")
    .WithExplicitStart();

builder.AddJavaScriptApp("web", "../../web/bookstore-web", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
