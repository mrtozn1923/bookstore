using Bookstore.Data;
using Bookstore.MigrationService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.AddSqlServerDbContext<BookstoreContext>("bookstoredb");
builder.Services.AddHostedService<MigrationWorker>();

builder.Build().Run();
