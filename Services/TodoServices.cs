using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

using Todo;
using bamboo_grpc;
using bamboo_grpc.Interfaces;
using bamboo_grpc.Repositories;

namespace bamboo_grpc.Services;

public class TodoService : Todo.Todo.TodoBase
{
    private readonly ITodosRepository _repository;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodosRepository repository, ILoggerFactory loggerFactory)
    {
        this._repository = repository;
        this._logger = loggerFactory.CreateLogger<TodoService>();
    }

    public override async Task<GetTodosReply> GetAll(
        Empty request,
        ServerCallContext context)
    {
        try
        {
            var todos = await _repository.GetTodos();
            var reply = new GetTodosReply();

            foreach (var todo in todos)
            {
                reply.Todos.Add(new GetTodoReply
                {
                    Id = todo.Id,
                    Title = todo.Title,
                    Description = todo.Description,
                    DueDate = Timestamp.FromDateTime(todo.DueDate.ToUniversalTime()),
                    Status = todo.Status,
                    Priority = todo.Priority
                });
            }
            _logger.LogInformation("GetAll method called");
            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetAll method failed: {ex}", ex);
            throw;
        }
    }

    public override async Task<GetTodoReply> Get(
        GetTodoRequest request,
        ServerCallContext context)
    {
        try
        {
            var todo = await _repository.GetTodo(request.Id);
            var reply = new GetTodoReply
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                DueDate = Timestamp.FromDateTime(todo.DueDate.ToUniversalTime()),
                Status = todo.Status,
                Priority = todo.Priority
            };
            _logger.LogInformation("Get method called");
            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError("Get method failed: {ex}", ex);
            throw;
        }
    }
                                                                                                                                                                
    public override async Task<Empty> Post(
        PostTodoRequest request,
        ServerCallContext context)
    {
        try {
            await _repository.InsertTodo(
                request.Title,
                request.Description,
                request.DueDate.ToDateTime(),
                request.Status,
                request.Priority
            );
            _logger.LogInformation("Post method called");
        }
        catch (Exception ex)
        {
            _logger.LogError("Post method failed: {ex}", ex);
            throw;
        }
    }

    public override async Task<Empty> Put(
        PutTodoRequest request,
        ServerCallContext context)
    {
        try
        {
            await _repository.UpdateTodo(
                request.Id,
                request.Title,
                request.Description,
                request.DueDate.ToDateTime(),
                request.Status,
                request.Priority
            );
            _logger.LogInformation("Put method called");
        }
        catch (Exception ex)
        {
            _logger.LogError("Put method failed: {ex}", ex);
            throw;
        }
    }

    public override async Task<Empty> Delete(
        DeleteTodoRequest request,
        ServerCallContext context)
    {   
        try
        {
            await _repository.DeleteTodo(request.Id);
            _logger.LogInformation("Delete method called");
            return new Empty();
        }
        catch (Exception ex)
        {
            _logger.LogError("Delete method failed: {ex}", ex);
            throw;
        }
    }
}
