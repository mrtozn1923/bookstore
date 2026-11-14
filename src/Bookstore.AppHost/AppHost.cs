IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

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

IResourceBuilder<ParameterResource> backofficeSecret = builder.AddParameter("backoffice-client-secret", secret: true);

// Realm import is for local development only; it is not part of publish/deploy
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithContainerName("aspire-keycloak")
    .WithRealmImport("./Realms")
    .WithEnvironment("BOOKSTORE_BACKOFFICE_SECRET", backofficeSecret);

IResourceBuilder<ProjectResource> migrations = builder.AddProject<Projects.Bookstore_MigrationService>("migrations")
    .WithEnvironment("Seed__GeneratedBooks", "200000")
    .WithReference(bookstoreDb)
    .WaitFor(bookstoreDb);

// Redis and Keycloak are references but not WaitFor: the catalog reads without them
IResourceBuilder<ProjectResource> catalog = builder.AddProject<Projects.Bookstore_Api>("catalog")
    .WithReference(bookstoreDb)
    .WithReference(redis)
    .WithReference(keycloak)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

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
    .WithHttpHealthCheck("/health");

// Catches order e-mails locally; started from the dashboard only when needed
builder.AddMailpit("mailpit")
    .WithContainerName("aspire-mailpit")
    .WithExplicitStart();

builder.AddJavaScriptApp("web", "../../web/bookstore-web", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
