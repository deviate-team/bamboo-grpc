using MongoDB.Driver;
using bamboo_grpc.Models;

namespace bamboo_grpc.Interfaces;

public interface ITodoContext
{
    IMongoCollection<TodoModel> Todos { get; }
}
