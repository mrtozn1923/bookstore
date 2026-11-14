IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// `aspire publish` writes a Helm chart for this environment (post 14). `aspire run` ignores it.
builder.AddKubernetesEnvironment("k8s");

// One SQL Server container locally, one database per service.
IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql")
    .WithImageTag("2025-latest")
    .WithContainerName("msapp-sql")
    .WithDataVolume("msapp-sql-data")
    .WithLifetime(ContainerLifetime.Persistent)
    // Without a limit SQL Server sizes itself for all the memory Docker's VM has and competes with every other
    // container there; on a developer machine 1 GB is plenty for four small databases.
    .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "1024");

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

// Generated once, kept in the AppHost's user secrets, never in the repository. The realm file refers to them
// as ${...} placeholders, which Keycloak fills from its environment during the import.
IResourceBuilder<ParameterResource> bffClientSecret = AddGeneratedSecret(builder, "bff-client-secret");
IResourceBuilder<ParameterResource> bffServiceSecret = AddGeneratedSecret(builder, "bff-service-secret");
IResourceBuilder<ParameterResource> demoPassword = AddGeneratedSecret(builder, "keycloak-demo-password");

// Local identity provider with the "bookstore" realm imported from Realms/. Production uses a managed Keycloak
// (or another OIDC provider) with its own realm management; the import is for development only.
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak", 8180)
    .WithContainerName("msapp-keycloak")
    .WithDataVolume("msapp-keycloak-data")
    .WithLifetime(ContainerLifetime.Persistent)
    // Keycloak's JVM takes a share of all the memory it sees by default; a fixed heap is enough locally.
    .WithEnvironment("JAVA_OPTS_KC_HEAP", "-Xms128m -Xmx512m")
    .WithEnvironment("BOOKSTORE_BFF_CLIENT_SECRET", bffClientSecret)
    .WithEnvironment("BOOKSTORE_BFF_SERVICE_SECRET", bffServiceSecret)
    .WithEnvironment("BOOKSTORE_DEMO_PASSWORD", demoPassword);
// The realm import is a bind mount of Realms/; the Kubernetes publisher does not support bind mounts,
// and production manages its realm elsewhere anyway (post 10).
if (builder.ExecutionContext.IsRunMode)
{
    keycloak.WithRealmImport("./Realms");
}

// The migrator is the Catalog binary started with --migrate; it exits when the schema is up to date.
IResourceBuilder<ProjectResource> catalogMigrator = builder
    .AddProject<Projects.Bookstore_Catalog_Api>("catalog-migrator", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithEnvironment("Seed__SampleData", "true")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

IResourceBuilder<ProjectResource> catalog = builder.AddProject<Projects.Bookstore_Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WithReference(keycloak)
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
    .WithReference(keycloak)
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
IResourceBuilder<ProjectResource> ordering = builder.AddProject<Projects.Bookstore_Ordering_Api>("ordering")
    .WithReference(orderingDb)
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitForCompletion(orderingMigrator)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health");

// Shared secret for the provider's webhook signatures (Standard Webhooks: "whsec_" + base64).
// 32 letters and digits are valid base64 (24 bytes); Aspire generates it once and keeps it in user secrets.
IResourceBuilder<ParameterResource> webhookSecret = AddGeneratedSecret(builder, "payment-webhook-secret");

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

// A fake SMTP server: it accepts every e-mail and shows it in a web UI instead of delivering it.
IResourceBuilder<ContainerResource> mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.33")
    .WithContainerName("msapp-mailpit")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp")
    .WithHttpEndpoint(targetPort: 8025, name: "http");
EndpointReference smtp = mailpit.GetEndpoint("smtp");

// Pushes order status changes to the browser (SignalR, behind the BFF) and sends the e-mails.
IResourceBuilder<ProjectResource> notifications = builder.AddProject<Projects.Bookstore_Notifications_Api>("notifications")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(keycloak)
    .WithEnvironment("Smtp__Host", smtp.Property(EndpointProperty.Host))
    .WithEnvironment("Smtp__Port", smtp.Property(EndpointProperty.Port))
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(mailpit)
    .WithHttpHealthCheck("/health");

// The only entry point for the browser. The services behind it keep their ports for local debugging;
// in production (post 14) only the BFF is exposed.
IResourceBuilder<ProjectResource> bff = builder.AddProject<Projects.Bookstore_Bff>("bff")
    .WithReference(catalog)
    .WithReference(ordering)
    .WithReference(inventory)
    .WithReference(notifications)
    .WithReference(keycloak)
    .WithEnvironment("Keycloak__ClientSecret", bffClientSecret)
    .WithEnvironment("Keycloak__ServiceClientSecret", bffServiceSecret)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// The Angular application. Locally "npm run watch" rebuilds it into the BFF's wwwroot on every change and the BFF
// serves it from the same origin as the API. Its production build goes into the BFF's image (post 14).
if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp("web", "../../web/bookstore-web", "watch")
        .WithParentRelationship(bff);
}

builder.Build().Run();

static IResourceBuilder<ParameterResource> AddGeneratedSecret(IDistributedApplicationBuilder builder, string name) =>
    builder.AddResource(ParameterResourceBuilderExtensions.CreateGeneratedParameter(
        builder, name, secret: true,
        new GenerateParameterDefault { MinLength = 32, Lower = true, Upper = true, Numeric = true, Special = false }));
