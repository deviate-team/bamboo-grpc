using bamboo_grpc.Models;
using bamboo_grpc.MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;

namespace bamboo_grpc.Repositories;

public class TodosRepository : ITodosRepository
{
  private readonly ILogger<TodosRepository> _logger;
  private readonly IMongoCollection<TodoModel> _todos;
  private readonly ConnectionMultiplexer _redisContext;

  public TodosRepository(
      ILoggerFactory loggerFactory,
      IMongoDBContext mongoDBContext,
      ConnectionMultiplexer redisContext
  )
  {
    this._logger = loggerFactory.CreateLogger<TodosRepository>();
    this._todos = mongoDBContext.GetCollection<TodoModel>("todos");
    this._redisContext = redisContext;
  }

  public async Task<IEnumerable<TodoModel>> GetTodos()
  {
    try
    {
      // Try to get data from Redis cache
      string cacheKey = "todos:all";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "[]")
      {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheData);
      }

      // If cache miss, fetch data from MongoDB
      var todos = await _todos.Find(new BsonDocument()).ToListAsync();

      // Cache the data in Redis for future use
      await _redisContext
          .GetDatabase()
          .StringSetAsync(cacheKey, Newtonsoft.Json.JsonConvert.SerializeObject(todos));

      return todos;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get todos: {ex}");
      return null;
    }
  }

  public async Task<TodoModel> GetTodoById(string id)
  {
    try
    {

      // Try to get data from Redis cache
      string cacheKey = $"todos:{id}";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData))
      {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<TodoModel>(cacheData);
      }

      // If cache miss, fetch data from MongoDB
      FilterDefinition<TodoModel> filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
      var todo = await _todos.Find(filter).FirstOrDefaultAsync();

      // Cache the data in Redis for future use
      await _redisContext
          .GetDatabase()
          .StringSetAsync(cacheKey, Newtonsoft.Json.JsonConvert.SerializeObject(todo));

      return todo;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get todo with id {id}: {ex}");
      return null;
    }
  }

  public async Task InsertTodo(
      string title,
      string description,
      string due_date,
      string status,
      string priority
  )
  {
    try
    {
      var todo = new TodoModel
      {
        Title = title,
        Description = description,
        DueDate = due_date,
        Status = status,
        Priority = priority
      };
      await _todos.InsertOneAsync(todo);
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to insert todo: {ex}");
    }
  }

  public async Task UpdateTodo(
      string id,
      string title,
      string description,
      string due_date,
      string status,
      string priority
  )
  {
    try
    {
      FilterDefinition<TodoModel> filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
      UpdateDefinition<TodoModel> update = Builders<TodoModel>.Update
          .Set(m => m.Title, title)
          .Set(m => m.Description, description)
          .Set(m => m.DueDate, due_date)
          .Set(m => m.Status, status)
          .Set(m => m.Priority, priority);

      await _todos.UpdateOneAsync(filter, update);
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to update todo with id {id}: {ex}");
    }
  }

  public async Task DeleteTodoById(string id)
  {
    try
    {
      FilterDefinition<TodoModel> filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
      await _todos.DeleteOneAsync(filter);
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to delete todo with id {id}: {ex}");
    }
  }
}
