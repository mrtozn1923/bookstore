using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

public static class BookstoreMongo
{
    private static int _registered;

    // Serialization settings are global: register them once, before the first use of any class
    public static void RegisterSerialization()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;   // tests and hosts may call this more than once
        }
        ConventionPack conventions =
        [
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        ];
        ConventionRegistry.Register("bookstore", conventions, type => type.Namespace == "Bookstore.MongoLab");
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public static IServiceCollection AddBookstoreMongo(this IServiceCollection services, string connectionString)
    {
        // One MongoClient for the whole application: it owns the connection pools and server monitoring
        services.AddSingleton<IMongoClient>(_ =>
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ApplicationName = "Bookstore.MongoLab";
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);
            settings.MaxConnectionPoolSize = 100;
            return new MongoClient(settings);
        });
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("bookstore"));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>().GetCollection<Book>("books"));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>().GetCollection<Order>("orders"));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>().GetCollection<Review>("reviews"));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>().GetCollection<Author>("authors"));
        return services;
    }
}
