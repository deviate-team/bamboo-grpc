using bamboo_grpc.Models;
using bamboo_grpc.MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using Newtonsoft.Json;

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
            string cacheKey = "todos:all";
            string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cacheData) && cacheData != "[]")
            {
                _logger.LogInformation("Get Data from cache..");
                return JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheData);
            }

            var todos = await _todos.Find(_ => true).ToListAsync();

            if (todos != null)
            {
                _logger.LogInformation("cache not found.. Get Data from Mongo..");
                await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(todos));
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
            string cacheKey = $"todos:{id}";
            string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cacheData) && cacheData != "{}")
            {
                _logger.LogInformation("Get Data by ID from cache..");
                return JsonConvert.DeserializeObject<TodoModel>(cacheData);
            }

            var filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
            var todo = await _todos.Find(filter).FirstOrDefaultAsync();

            if (todo != null)
            {
                _logger.LogInformation("cache not found.. Get Data by ID from Mongo..");
                await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(todo));
            }

            return todo;
        }
        catch
        {
            _logger.LogError($"Failed to get todo with id {id}");
            throw;
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
            var filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
            var existingTodo = await _todos.Find(filter).FirstOrDefaultAsync();

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
            await _todos.ReplaceOneAsync(filter, existingTodo);

            // Update the cached data for the individual todo
            var cacheKey = $"todos:{id}";
            await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(existingTodo));

            // Update the cached data for the entire list of todos
            var cacheKeyAll = "todos:all";
            var cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);
            if (!string.IsNullOrEmpty(cacheDataAll))
            {
                var todos = JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheDataAll);
                var updatedTodos = todos.Select(todo => {
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

    public async Task DeleteTodoById(string id)
    {
        try
        {
            var filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
            var result = await _todos.DeleteOneAsync(filter);
            if (result.DeletedCount == 1)
            {
                string cacheKey = $"todos:{id}";
                await _redisContext.GetDatabase().KeyDeleteAsync(cacheKey);

                // Update the cached data for the entire list of todos
                string cacheKeyAll = "todos:all";
                string cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);
                if (cacheDataAll != null && cacheDataAll.ToString() != "[]")
                {
                    IEnumerable<TodoModel> todos = JsonConvert.DeserializeObject<IEnumerable<TodoModel>>(cacheDataAll);
                    int index = todos.ToList().FindIndex(m => m.Id == id);
                    if (index != -1)
                    {
                        todos = todos.Where(m => m.Id != id);
                        await _redisContext.GetDatabase().StringSetAsync(cacheKeyAll, JsonConvert.SerializeObject(todos));
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
