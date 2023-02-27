using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

using Todo;
using bamboo_grpc;
using bamboo_grpc.Interfaces;

namespace bamboo_grpc.Services;

public class TodoService : Todo.Todo.TodoBase
{
    private readonly ITodosRepository repository;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodosRepository repository, ILoggerFactory loggerFactory)
    {
        this.repository = repository;
        this._logger = loggerFactory.CreateLogger<TodoService>();
    }

    public override Task<GetTodosReply> GetAll(
        Empty request,
        ServerCallContext context)
    {
        var result = new GetTodosReply();

        result.Todos.AddRange(repository.GetTodos()
            .Select(i => new GetTodoReply
            {
                Id = i.id,
                Description = i.description
            }));

        _logger.LogInformation("GetAll method called");
        return Task.FromResult(result);
    }

    public override Task<GetTodoReply> Get(
        GetTodoRequest request,
        ServerCallContext context)
    {
        var todoDescription = repository.GetTodo(request.Id);
        _logger.LogInformation("Get method called for ID {id}", request.Id);

        return Task.FromResult(new GetTodoReply
        {
            Id = request.Id,
            Description = todoDescription
        });
    }
    //                                                                                                                                                                             
    public override Task<Empty> Post(
        PostTodoRequest request,
        ServerCallContext context)
    {
        repository.InsertTodo(request.Description);
        _logger.LogInformation("Post method called with description {description}", request.Description);

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> Put(
        PutTodoRequest request,
        ServerCallContext context)
    {
        repository.UpdateTodo(request.Id, request.Description);
        _logger.LogInformation("Put method called for ID {id}, Description {description}", request.Id, request.Description);

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> Delete(
        DeleteTodoRequest request,
        ServerCallContext context)
    {
        repository.DeleteTodo(request.Id);
        _logger.LogInformation("Delete method called for ID {id}", request.Id);
        return Task.FromResult(new Empty());
    }
}
