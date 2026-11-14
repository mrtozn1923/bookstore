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

IResourceBuilder<ProjectResource> catalog = builder.AddProject<Projects.Bookstore_Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WaitForCompletion(catalogMigrator)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

IResourceBuilder<ProjectResource> inventoryMigrator = builder
    .AddProject<Projects.Bookstore_Inventory_Grpc>("inventory-migrator", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithEnvironment("Seed__SampleData", "true")
    .WithReference(inventoryDb)
    .WaitFor(inventoryDb);

// Two endpoints: "http" from the launch profile (health checks) and "grpc". gRPC without TLS needs
// a port that speaks only HTTP/2, so Kestrel gets both endpoints, with their protocols, from the AppHost.
IResourceBuilder<ProjectResource> inventory = builder.AddProject<Projects.Bookstore_Inventory_Grpc>("inventory")
    .WithHttpEndpoint(name: "grpc")
    .WithEndpoint("grpc", endpoint => endpoint.Transport = "http2")
    .WithReference(inventoryDb)
    .WaitForCompletion(inventoryMigrator)
    .WithHttpHealthCheck("/health");
EndpointReference inventoryHttp = inventory.GetEndpoint("http");
EndpointReference inventoryGrpc = inventory.GetEndpoint("grpc");
inventory
    .WithEnvironment("Kestrel__Endpoints__Http__Url",
        ReferenceExpression.Create($"http://localhost:{inventoryHttp.Property(EndpointProperty.TargetPort)}"))
    .WithEnvironment("Kestrel__Endpoints__Grpc__Url",
        ReferenceExpression.Create($"http://localhost:{inventoryGrpc.Property(EndpointProperty.TargetPort)}"))
    .WithEnvironment("Kestrel__Endpoints__Grpc__Protocols", "Http2");

// Ordering's migrator also fills the local book copy once, from Catalog's API.
IResourceBuilder<ProjectResource> orderingMigrator = builder
    .AddProject<Projects.Bookstore_Ordering_Api>("ordering-migrator", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithEnvironment("CatalogCopy__ImportWhenEmpty", "true")
    .WithReference(orderingDb)
    .WithReference(catalog)
    .WaitFor(orderingDb)
    .WaitFor(catalog);

// Ordering calls Inventory at runtime, but does not wait for it to start: they start independently.
builder.AddProject<Projects.Bookstore_Ordering_Api>("ordering")
    .WithReference(orderingDb)
    .WithReference(inventory)
    .WaitForCompletion(orderingMigrator)
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
