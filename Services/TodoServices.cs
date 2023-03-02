using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Todo;
using bamboo_grpc.Repositories;

namespace bamboo_grpc.Services
{
    public class TodoService : Todo.Todo.TodoBase
    {
        private readonly ITodosRepository _repository;
        private readonly ILogger<TodoService> _logger;

        public TodoService(ITodosRepository repository, ILogger<TodoService> logger)
        {
            this._repository = repository;
            this._logger =  logger;
        }

        public override async Task<GetTodosReply> GetAll(Empty request, ServerCallContext context)
        {
            var response = new GetTodosReply();
            response.Todos.AddRange(
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
            return response;
        }

        public override async Task<GetTodoReply> Get(
            GetTodoRequest request,
            ServerCallContext context
        )
        {
            var todo = await _repository.GetTodoById(request.Id);
            var response = new GetTodoReply
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                DueDate = todo.DueDate,
                Status = todo.Status,
                Priority = todo.Priority,
            };
            _logger.LogInformation("Get method called");
            return response;
        }

        public override async Task<Empty> Post(PostTodoRequest request, ServerCallContext context)
        {
            await _repository.InsertTodo(
                request.Title,
                request.Description,
                request.DueDate,
                request.Status,
                request.Priority
            );
            _logger.LogInformation("Post method called");
            return new Empty();
        }

        public override async Task<Empty> Put(PutTodoRequest request, ServerCallContext context)
        {
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

        public override async Task<Empty> Delete(
            DeleteTodoRequest request,
            ServerCallContext context
        )
        {
            await _repository.DeleteTodoById(request.Id);
            _logger.LogInformation("Delete method called");
            return new Empty();
        }
    }
}
