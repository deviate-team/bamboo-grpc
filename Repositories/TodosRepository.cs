using MongoDB.Driver;
using StackExchange.Redis;
using Newtonsoft.Json;
using bamboo_grpc.Models;
using bamboo_grpc.Interfaces;

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
      // check cache
      string cacheKey = "todos:all";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "[]")
      {
        _logger.LogInformation("Get Data from cache..");
        return JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheData);
      }

      // get data from database
      var todos = await _todos.Find(_ => true).ToListAsync();
      if (todos != null)
      {
        _logger.LogInformation("cache not found.. Get Data from Mongo..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(todos), TimeSpan.FromMinutes(15));
      }
      return todos;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get todos: {ex}");
      throw;
    }
  }
  public async Task<TodoModel> GetTodoById(string id)
  {
    try
    {
      // check cache
      string cacheKey = $"todos:{id}";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "{}")
      {
        _logger.LogInformation("Get Data by ID from cache..");
        return JsonConvert.DeserializeObject<TodoModel>(cacheData);
      }

      // get data from database
      var todo = await _todos.Find(_ => _.Id == id).FirstOrDefaultAsync();
      if (todo != null)
      {
        _logger.LogInformation("cache not found.. Get Data by ID from Mongo..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(todo), TimeSpan.FromMinutes(15));
      }

      return todo;
    }
    catch
    {
      _logger.LogError($"Failed to get todo with id {id}");
      throw;
    }
  }

  public async Task<IEnumerable<TodoModel>> GetTodosByUserId(string userId)
  {
    try
    {
      string cacheKey = $"todos:all:{userId}";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "[]")
      {
        _logger.LogInformation("Get Data by User ID from cache..");
        return JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheData);
      }
      var todos = await _todos.Find(_ => _.UserId == userId).ToListAsync();
      if (todos != null)
      {
        _logger.LogInformation("cache not found.. Get Data by User ID from Mongo..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(todos), TimeSpan.FromMinutes(15));
      }

      return todos;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get todos: {ex}");
      throw;
    }
  }

  public async Task InsertTodo(
      string title,
      string description,
      string due_date,
      string status,
      string priority,
      string userId
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
        Priority = priority,
        UserId = userId
      };
      // Insert the new todo into the database
      await _todos.InsertOneAsync(todo);

      // Update the cached data for the entire list of todos
      string cacheKey = "todos:all";
      await _redisContext.GetDatabase().KeyDeleteAsync(cacheKey);
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to insert todo: {ex}");
      throw;
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
      // Find the existing todo in the database
      var existingTodo = await _todos.Find(_ => _.Id == id).FirstOrDefaultAsync();
      if (existingTodo == null)
      {
        throw new Exception("Todo not found");
      }

      // Update the existing todo in the database
      existingTodo.Title = title;
      existingTodo.Description = description;
      existingTodo.DueDate = due_date;
      existingTodo.Status = status;
      existingTodo.Priority = priority;
      await _todos.ReplaceOneAsync(_ => _.Id == id, existingTodo);

      // Update the cached data for the individual todo
      var cacheKey = $"todos:{id}";
      await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(existingTodo));

      // Update the cached data for the entire list of todos
      var cacheKeyAll = "todos:all";
      var cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);
      if (!string.IsNullOrEmpty(cacheDataAll))
      {
        var todos = JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheDataAll);
        var updatedTodos = todos.Select(todo =>
        {
          if (todo.Id == id)
          {
            todo.Title = title;
            todo.Description = description;
            todo.DueDate = due_date;
            todo.Status = status;
            todo.Priority = priority;
          }
          return todo;
        });
        await _redisContext.GetDatabase().StringSetAsync(cacheKeyAll, JsonConvert.SerializeObject(updatedTodos));
      }
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to update todo with id {id}: {ex}");
      throw;
    }
  }

  public async Task DeleteTodoById(string id, string userId)
  {
    try
    {
      // Delete the todo from the database
      var result = await _todos.DeleteOneAsync(_ => _.Id == id);
      if (result.DeletedCount == 1)
      {
        string cacheKey = $"todos:{id}";
        await _redisContext.GetDatabase().KeyDeleteAsync(cacheKey);

        // Update the cached data for the entire list of todos
        string cacheKeyAll = "todos:all";
        string cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);
        if (!string.IsNullOrEmpty(cacheDataAll) && cacheDataAll != "[]")
        {
          IEnumerable<TodoModel> todos = JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheDataAll);
          int index = todos.ToList().FindIndex(m => m.Id == id);
          if (index != -1)
          {
            todos = todos.Where(m => m.Id != id);
            await _redisContext.GetDatabase().StringSetAsync(cacheKeyAll, JsonConvert.SerializeObject(todos));
          }
        }

        // Update the cached data for the entire list of todos by user id
        string cacheKeyAllByUserId = $"todos:all:{userId}";
        string cacheDataAllByUserId = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAllByUserId);
        if (!string.IsNullOrEmpty(cacheDataAllByUserId) && cacheDataAllByUserId != "[]")
        {
          IEnumerable<TodoModel> todos = JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheDataAllByUserId);
          int index = todos.ToList().FindIndex(m => m.Id == id);
          if (index != -1)
          {
            todos = todos.Where(m => m.Id != id);
            await _redisContext.GetDatabase().StringSetAsync(cacheKeyAllByUserId, JsonConvert.SerializeObject(todos));
          }
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to delete todo with id {id}: {ex}");
      throw;
    }
  }
}
