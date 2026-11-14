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

// Nothing uses Keycloak before post 10. Explicit start keeps its ~500 MB free until then:
// start it from the dashboard (or `aspire resource keycloak start`) when you need it.
builder.AddKeycloak("keycloak", 8180)
    .WithContainerName("msapp-keycloak")
    .WithDataVolume("msapp-keycloak-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExplicitStart();

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
    .WithReference(rabbitmq)
    .WaitForCompletion(catalogMigrator)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
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
    .WithReference(rabbitmq)
    .WaitForCompletion(inventoryMigrator)
    .WaitFor(rabbitmq)
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

// Since post 07 Ordering talks to Inventory and Payments only through RabbitMQ.
builder.AddProject<Projects.Bookstore_Ordering_Api>("ordering")
    .WithReference(orderingDb)
    .WithReference(rabbitmq)
    .WaitForCompletion(orderingMigrator)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

// Shared secret for the provider's webhook signatures (Standard Webhooks: "whsec_" + base64).
// 32 letters and digits are valid base64 (24 bytes); Aspire generates it once and keeps it in user secrets.
IResourceBuilder<ParameterResource> webhookSecret = builder.AddResource(ParameterResourceBuilderExtensions.CreateGeneratedParameter(
    builder, "payment-webhook-secret", secret: true,
    new GenerateParameterDefault { MinLength = 32, Lower = true, Upper = true, Numeric = true, Special = false }));

// Stands in for the external card payment provider. In production this is the provider's URL, not a resource we run.
IResourceBuilder<ProjectResource> paymentProvider = builder.AddProject<Projects.Bookstore_FakePaymentProvider>("payment-provider")
    .WithEnvironment("Webhooks__Secret", ReferenceExpression.Create($"whsec_{webhookSecret}"));

IResourceBuilder<ProjectResource> paymentsMigrator = builder
    .AddProject<Projects.Bookstore_Payments_Api>("payments-migrator", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithReference(paymentsDb)
    .WaitFor(paymentsDb);

IResourceBuilder<ProjectResource> payments = builder.AddProject<Projects.Bookstore_Payments_Api>("payments")
    .WithReference(paymentsDb)
    .WithReference(rabbitmq)
    .WithReference(paymentProvider)
    .WithEnvironment("PaymentProvider__WebhookSecret", ReferenceExpression.Create($"whsec_{webhookSecret}"))
    .WaitForCompletion(paymentsMigrator)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

// The provider calls Payments back; in production this URL is registered in the provider's dashboard.
paymentProvider.WithEnvironment("Webhooks__Url",
    ReferenceExpression.Create($"{payments.GetEndpoint("http")}/webhooks/payment-provider"));

builder.AddProject<Projects.Bookstore_Notifications_Api>("notifications")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
