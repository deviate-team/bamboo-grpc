using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TodoServer;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace TodoServer.Services
{
    public class TodoListService : TodoService.TodoServiceBase
    { 
        private readonly IDatabase _database;

        public TodoListService()
        {
            var redis = ConnectionMultiplexer.Connect("redis:6379");
            _database = redis.GetDatabase();
        }

        public override Task<TodoItem> CreateTodo(TodoItem request, ServerCallContext context)
        {
            request.Id = GetNextId();
            var serialized = JsonConvert.SerializeObject(request);
            _database.StringSet($"todo:{request.Id}", serialized);
            return Task.FromResult(request);
        }

        public override Task<TodoItem> ReadTodo(TodoId request, ServerCallContext context)
        {
            var serialized = _database.StringGet($"todo:{request.Id}");
            var todo = JsonConvert.DeserializeObject<TodoItem>(serialized);
            return Task.FromResult(todo);
        }

        public override Task<TodoItem> UpdateTodo(TodoItem request, ServerCallContext context)
        {
            var serialized = JsonConvert.SerializeObject(request);
            _database.StringSet($"todo:{request.Id}", serialized);
            return Task.FromResult(request);
        }

        public override Task<Empty> DeleteTodo(TodoId request, ServerCallContext context)
        {
            _database.KeyDelete($"todo:{request.Id}");
            return Task.FromResult(new Empty());
        }

        private int GetNextId()
        {
            return (int)_database.StringIncrement("todo:id");
        }
    }
}