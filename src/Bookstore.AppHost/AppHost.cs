IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> catalog = builder.AddProject<Projects.Bookstore_Api>("catalog");

builder.AddJavaScriptApp("web", "../../web/bookstore-web", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(catalog)
    .WaitFor(catalog);

builder.Build().Run();
