using Microsoft.AspNetCore.Authorization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Todo;
using bamboo_grpc.Interfaces;

namespace bamboo_grpc.Services
{
  [Authorize]
  public class TodoService : Todo.Todo.TodoBase
  {
    private readonly ITodosRepository _repository;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodosRepository repository, ILogger<TodoService> logger)
    {
      this._repository = repository;
      this._logger = logger;
    }

    [Authorize(Roles = "Administrator")]
    public override async Task<GetTodosReply> GetAll(Empty request, ServerCallContext context)
    {
      try
      {
        var reply = new GetTodosReply();
        reply.Todos.AddRange(
            (await _repository.GetTodos()).Select(
                t => new GetTodoReply
                {
                  Id = t.Id,
                  Title = t.Title,
                  Description = t.Description,
                  DueDate = t.DueDate,
                  Status = t.Status,
                  Priority = t.Priority,
                }
            )
        );

        _logger.LogInformation("GetAll method called");
        return reply;
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
        _logger.LogError($"Failed to get todos: {ex}");
      }
    }


    public override async Task<GetTodosReply> GetByUserId(
        Empty request,
        ServerCallContext context
    )
    {
      try
      {
        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);

        var reply = new GetTodosReply();
        reply.Todos.AddRange(
            (await _repository.GetTodosByUserId(userId)).Select(
                todo => new GetTodoReply
                {
                  Id = todo.Id,
                  Title = todo.Title,
                  Description = todo.Description,
                  DueDate = todo.DueDate,
                  Status = todo.Status,
                  Priority = todo.Priority,
                }
            )
        );

        _logger.LogInformation("GetByUserId method called");
        return reply;
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
        _logger.LogError($"Failed to get todos: {ex}");
      }
    }

    public override async Task<GetTodoReply> Get(
        GetTodoRequest request,
        ServerCallContext context
    )
    {
      try
      {
        if (string.IsNullOrEmpty(request.Id))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Id is required"));
        }

        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);

        var todo = await _repository.GetTodoById(request.Id);
        if (todo == null)
        {
          throw new RpcException(new Status(StatusCode.NotFound, "Todo not found"));
        }

        var reply = new GetTodoReply
        {
          Id = todo.Id,
          Title = todo.Title,
          Description = todo.Description,
          DueDate = todo.DueDate,
          Status = todo.Status,
          Priority = todo.Priority,
        };
        _logger.LogInformation("Get method called");
        return reply;
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
        _logger.LogError($"Failed to get todo: {ex}");
      }
    }

    public override async Task<Empty> Post(PostTodoRequest request, ServerCallContext context)
    {
      try
      {
        if (string.IsNullOrEmpty(request.Title))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Title is required"));
        }

        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);

        await _repository.InsertTodo(
          request.Title,
          request.Description,
          request.DueDate,
          request.Status,
          request.Priority,
          userId
      );
        _logger.LogInformation("Post method called");
        return new Empty();
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
        _logger.LogError(ex, $"failed to insert todo ex: {ex}");
      }
    }

    public override async Task<Empty> Put(PutTodoRequest request, ServerCallContext context)
    {
      try
      {
        if (string.IsNullOrEmpty(request.Id))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Id is required"));
        }

        if (string.IsNullOrEmpty(request.Title))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Title is required"));
        }

        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);

        var existingTodo = await _repository.GetTodoById(request.Id);
        if (existingTodo == null)
        {
          throw new RpcException(new Status(StatusCode.NotFound, "Todo not found"));
        }
        if (existingTodo.UserId != userId)
        {
          throw new RpcException(new Status(StatusCode.PermissionDenied, "You are not allowed to update this todo"));
        }
        await _repository.UpdateTodo(
          request.Id,
          request.Title,
          request.Description,
          request.DueDate,
          request.Status,
          request.Priority,
          userId
      );
        _logger.LogInformation("Put method called");
        return new Empty();
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
        _logger.LogError(ex, $"failed to update todo ex: {ex}");
      }
    }

    public override async Task<Empty> Delete(
        DeleteTodoRequest request,
        ServerCallContext context
    )
    {
      try
      {
        if (string.IsNullOrEmpty(request.Id))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Id is required"));
        }

        var existingTodo = await _repository.GetTodoById(request.Id);
        if (existingTodo == null)
        {
          throw new RpcException(new Status(StatusCode.NotFound, "Todo not found"));
        }

        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);
        if (existingTodo.UserId != userId)
        {
          throw new RpcException(new Status(StatusCode.PermissionDenied, "You are not allowed to delete this todo"));
        }

        await _repository.DeleteTodoById(request.Id, userId);
        _logger.LogInformation("Delete method called");
        return new Empty();
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
        _logger.LogError(ex, $"failed to delete todo ex: {ex}");
      }
    }
  }
}
