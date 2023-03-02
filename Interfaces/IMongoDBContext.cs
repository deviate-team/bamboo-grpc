using MongoDB.Driver;

namespace bamboo_grpc.Interfaces;

public interface IMongoDBContext
{
    IMongoCollection<T> GetCollection<T>(string name);
}
