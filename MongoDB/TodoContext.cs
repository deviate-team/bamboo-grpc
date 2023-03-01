using bamboo_grpc;
using bamboo_grpc.Models;
using bamboo_grpc.Interfaces;
using MongoDB.Driver;

namespace bamboo_grpc.MongoDB;

public class TodoContext : ITodoContext
{
    private readonly IMongoDatabase _db;
    public TodoContext(MongoDBConfig config)
    {
        var client = new MongoClient(config.ConnectionString);
        _db = client.GetDatabase(config.Database);
    }
    public IMongoCollection<TodoModel> Todos => _db.GetCollection<TodoModel>("Todos");
}
