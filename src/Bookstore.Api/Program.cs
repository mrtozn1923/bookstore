using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Bookstore.Api.Uploads;

var builder = WebApplication.CreateBuilder(args);

// The real scheme comes from the Authentication and Authorization series; tests register their own
builder.Services.AddAuthentication();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Publisher", policy => policy.RequireClaim("publisher_id"));

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOptions<UploadOptions>().BindConfiguration("Uploads");
builder.Services.AddOptions<ClamAvOptions>().BindConfiguration("ClamAv");
builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration["Storage:ConnectionString"]));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<UploadStore>();
builder.Services.AddSingleton<UploadQueue>();
builder.Services.AddSingleton<UploadSasIssuer>();
builder.Services.AddSingleton<EpubInspector>();
builder.Services.AddSingleton<CoverReencoder>();
builder.Services.AddSingleton<ClamAvScanner>();
builder.Services.AddHostedService<UploadProcessor>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapUploads();

app.Run();

public partial class Program;
