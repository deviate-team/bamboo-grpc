using MongoDB.Driver;
using bamboo_grpc.Interfaces;

namespace bamboo_grpc.MongoDB;

public class MongoDBContext : IMongoDBContext
{
    private readonly IMongoDatabase _database;

    public MongoDBContext()
    {
        // for environment variables
        var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "mongodb://localhost:27017";
        }

        var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME");
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = "bamboo";
        }

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
}
