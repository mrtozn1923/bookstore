using Bookstore.Search;
using Bookstore.SearchSync;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("Bookstore")
    ?? throw new InvalidOperationException("ConnectionStrings:Bookstore is missing.");

builder.Services.Configure<SearchSyncOptions>(builder.Configuration.GetSection(SearchSyncOptions.SectionName));
builder.Services.AddBookstoreSearch(builder.Configuration, isDevelopment: false);
builder.Services.AddSingleton(new BookChangeReader(connectionString));
builder.Services.AddHostedService<SearchSyncWorker>();

IHost host = builder.Build();
host.Run();
