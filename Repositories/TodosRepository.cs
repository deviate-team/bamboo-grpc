using bamboo_grpc.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace bamboo_grpc.Repositories
{
    internal class TodosRepository : ITodosRepository
    {
        private readonly IMongoCollection<Todo> todos;
        private readonly IDatabase cache;
        private int currentId = 1;

        public TodosRepository()
        {
            var mongoClient = new MongoClient("mongodb://admin:password@localhost:27017/mydatabase");
            var database = mongoClient.GetDatabase("mydatabase");
            todos = database.GetCollection<Todo>("todos");

            var redis = ConnectionMultiplexer.Connect("redis:6379");
            cache = redis.GetDatabase();
        }

        public IEnumerable<(int id, string description)> GetTodos()
        {
            var cachedResults = cache.Get<List<(int id, string description)>>("todos");

            if (cachedResults != null)
            {
                _logger.LogInformation("Retrieving todos from cache...");
                return cachedResults;
            }

            _logger.LogInformation("Retrieving todos from database...");

            var results = todos.Find(_ => true).ToList()
                .Select(todo => (todo.Id, todo.Description));

            cache.Set("todos", results, TimeSpan.FromMinutes(10));

            return results;
        }

        public string GetTodo(int id)
        {
            var cachedTodo = cache.Get<string>($"todo:{id}");

            if (cachedTodo != null)
            {
                _logger.LogInformation($"Retrieving todo {id} from cache...");
                return cachedTodo;
            }

            Console.WriteLine($"Retrieving todo {id} from database...");

            var todo = todos.Find(t => t.Id == id).FirstOrDefault();

            if (todo != null)
            {
                cache.Set($"todo:{id}", todo.Description, TimeSpan.FromMinutes(10));
                return todo.Description;
            }

            return null;
        }

        public void InsertTodo(string description)
        {
            var todo = new Todo { Id = currentId, Description = description };
            todos.InsertOne(todo);
            currentId++;

            cache.Remove("todos");
        }

        public void UpdateTodo(int id, string description)
        {
            var filter = Builders<Todo>.Filter.Eq(t => t.Id, id);
            var update = Builders<Todo>.Update.Set(t => t.Description, description);
            todos.UpdateOne(filter, update);

            cache.Remove($"todo:{id}");
            cache.Remove("todos");
        }

        public void DeleteTodo(int id)
        {
            var filter = Builders<Todo>.Filter.Eq(t => t.Id, id);
            todos.DeleteOne(filter);

            cache.Remove($"todo:{id}");
            cache.Remove("todos");
        }
    }
