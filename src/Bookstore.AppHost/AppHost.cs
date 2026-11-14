IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// One SQL Server container locally, one database per service.
IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql")
    .WithImageTag("2025-latest")
    .WithContainerName("msapp-sql")
    .WithDataVolume("msapp-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> catalogDb = sql.AddDatabase("catalogdb");
IResourceBuilder<SqlServerDatabaseResource> orderingDb = sql.AddDatabase("orderingdb");
IResourceBuilder<SqlServerDatabaseResource> inventoryDb = sql.AddDatabase("inventorydb");
IResourceBuilder<SqlServerDatabaseResource> paymentsDb = sql.AddDatabase("paymentsdb");

IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithContainerName("msapp-redis");

IResourceBuilder<RabbitMQServerResource> rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithContainerName("msapp-rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume("msapp-rabbitmq-data")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddKeycloak("keycloak", 8180)
    .WithContainerName("msapp-keycloak")
    .WithDataVolume("msapp-keycloak-data")
    .WithLifetime(ContainerLifetime.Persistent);

// The migrator is the Catalog binary started with --migrate; it exits when the schema is up to date.
IResourceBuilder<ProjectResource> catalogMigrator = builder
    .AddProject<Projects.Bookstore_Catalog_Api>("catalog-migrator", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithEnvironment("Seed__SampleData", "true")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

builder.AddProject<Projects.Bookstore_Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WaitForCompletion(catalogMigrator)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookstore_Ordering_Api>("ordering")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookstore_Inventory_Grpc>("inventory")
    .WithReference(inventoryDb)
    .WaitFor(inventoryDb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookstore_Payments_Api>("payments")
    .WithReference(paymentsDb)
    .WaitFor(paymentsDb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Bookstore_Notifications_Api>("notifications")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
