using bamboo_grpc.Interfaces;
using bamboo_grpc.Models;
using bamboo_grpc.MongoDB;

using MongoDB.Driver;
using MongoDB.Bson;

namespace bamboo_grpc.Repositories;

public class TodosRepository : ITodosRepository
{
    private readonly ILogger<TodosRepository> _logger;
    private readonly ITodoContext _context;

    public TodosRepository(ILoggerFactory loggerFactory, ITodoContext context)
    {
        this._context = context;
        this._logger = loggerFactory.CreateLogger<TodosRepository>();
    }

    public async Task<IEnumerable<TodoModel>> GetTodos()
    {
        try
        {
            return await _context
              .Todos
              .Find(_ => true)
              .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get todos: {ex}");
            return new List<TodoModel>();
        }
    }

    public async Task<IEnumerable<TodoModel>> GetTodo(string id)
    {
        try
        {
            FilterDefinition<TodoModel> filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
            return await _context
              .Todos
              .Find(filter)
              .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get todo with id {id}: {ex}");
            return new List<TodoModel>();
        }
    }

    public async Task InsertTodo(string title, string description, DateTime due_date, string status, string priority)
    {
        try
        {
            var todo = new TodoModel
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Description = description,
                DueDate = due_date,
                Status = status,
                Priority = priority
            };
            await _context.Todos.InsertOneAsync(todo);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to insert todo: {ex}");

        }
    }

    public async Task UpdateTodo(string id, string title, string description, DateTime due_date, string status, string priority)
    {
        try
        {
            FilterDefinition<TodoModel> filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
            var update = Builders<TodoModel>.Update
              .Set(m => m.Title, title)
              .Set(m => m.Description, description)
              .Set(m => m.Dueate, due_date)
              .Set(m => m.Status, status)
              .Set(m => m.Priority, priority);

            await _context.Todos.UpdateOneAsync(filter, update);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update todo with id {id}: {ex}");
        }
    }

    public async Task DeleteTodo(string id)
    {
        try
        {
            FilterDefinition<TodoModel> filter = Builders<TodoModel>.Filter.Eq(m => m.Id, id);
            await _context.Todos.DeleteOneAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update todo with id {id}: {ex}");
        }
    }
}
