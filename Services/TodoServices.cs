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
      }
    }

    public override async Task<GetTodoReply> Get(
        GetTodoRequest request,
        ServerCallContext context
    )
    {
      try
      {
        // check if id is empty
        if (string.IsNullOrEmpty(request.Id))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Id is required"));
        }
        // get todo
        var todo = await _repository.GetTodoById(request.Id);
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
      }
    }

    public override async Task<Empty> Post(PostTodoRequest request, ServerCallContext context)
    {
      try
      {
        // get user id from token
        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);

        // insert todo
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
      }
    }

    public override async Task<Empty> Put(PutTodoRequest request, ServerCallContext context)
    {
      try
      {
        // Check if id is provided
        if (string.IsNullOrEmpty(request.Id))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Id is required"));
        }

        // Check if todo exists
        var existingTodo = await _repository.GetTodoById(request.Id);
        if (existingTodo == null)
        {
          throw new RpcException(new Status(StatusCode.NotFound, "Todo not found"));
        }

        // Check if user is allowed to update todo
        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);
        if (existingTodo.UserId != userId)
        {
          throw new RpcException(new Status(StatusCode.PermissionDenied, "You are not allowed to update this todo"));
        }

        // Update todo
        await _repository.UpdateTodo(
          request.Id,
          request.Title,
          request.Description,
          request.DueDate,
          request.Status,
          request.Priority
      );
        _logger.LogInformation("Put method called");
        return new Empty();
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
      }
    }

    public override async Task<Empty> Delete(
        DeleteTodoRequest request,
        ServerCallContext context
    )
    {
      try
      {
        // Check if id is provided
        if (string.IsNullOrEmpty(request.Id))
        {
          throw new RpcException(new Status(StatusCode.InvalidArgument, "Id is required"));
        }

        // Check if todo exists
        var existingTodo = await _repository.GetTodoById(request.Id);
        if (existingTodo == null)
        {
          throw new RpcException(new Status(StatusCode.NotFound, "Todo not found"));
        }

        // Check if user is allowed to delete todo
        var token = context.RequestHeaders.GetValue("Authorization");
        var userId = JwtAuthentication.DecodeToken(token);
        if (existingTodo.UserId != userId)
        {
          throw new RpcException(new Status(StatusCode.PermissionDenied, "You are not allowed to delete this todo"));
        }

        // Delete todo
        await _repository.DeleteTodoById(request.Id, userId);
        _logger.LogInformation("Delete method called");
        return new Empty();
      }
      catch (Exception ex)
      {
        throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
      }
    }
  }
}
