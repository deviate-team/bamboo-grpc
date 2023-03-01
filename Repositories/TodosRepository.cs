using bamboo_grpc.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using Serilog;

namespace bamboo_grpc.Repositories
{
    internal class TodosRepository : ITodosRepository
    {
        private readonly ILogger<TodosRepository> _logger;
        private readonly ConnectionMultiplexer redis;
        private readonly IMongoCollection<BsonDocument> todosCollection;
        private int currentId = 1;

        public TodosRepository(ILoggerFactory loggerFactory)
        {

            this._logger = loggerFactory.CreateLogger<TodosRepository>();

            redis = ConnectionMultiplexer.Connect("redis:6379,password=redispassword");
            var mongoClient = new MongoClient("mongodb://root:example@mongo:27017");
            var database = mongoClient.GetDatabase("mydatabase");
            todosCollection = database.GetCollection<BsonDocument>("todos");
        }

        public IEnumerable<(int id, string description)> GetTodos()
        {
            var results = new List<(int id, string description)>();

            var redisTodos = redis.GetDatabase().StringGet("todos");
            if (redisTodos.HasValue)
            {
                results = Newtonsoft.Json.JsonConvert.DeserializeObject<List<(int id, string description)>>(redisTodos);
                _logger.LogInformation("Retrieved todos from Redis cache");
            }
            else
            {
                var todos = todosCollection.Find(new BsonDocument()).ToList();
                results = todos.Select(t => ((int)t["_id"], t["description"].AsString)).ToList();
                redis.GetDatabase().StringSet("todos", Newtonsoft.Json.JsonConvert.SerializeObject(results));
                _logger.LogInformation("Retrieved todos from MongoDB and added to Redis cache");
            }

            return results;
        }

        public string GetTodo(int id)
        {
            var redisTodo = redis.GetDatabase().HashGet("todo", id);
            if (redisTodo.HasValue)
            {
                _logger.LogInformation("Retrieved todo with id {Id} from Redis cache", id);
                return redisTodo;
            }
            else
            {
                var todo = todosCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstOrDefault();
                if (todo == null)
                {
                    _logger.LogInformation("Todo with id {Id} not found in MongoDB", id);
                    return null;
                }
                else
                {
                    redis.GetDatabase().HashSet("todo", id, todo["description"].AsString);
                    _logger.LogInformation("Retrieved todo with id {Id} from MongoDB and added to Redis cache", id);
                    return todo["description"].AsString;
                }
            }
        }

        public void InsertTodo(string description)
        {
            var todo = new BsonDocument
            {
                { "_id", currentId },
                { "description", description }
            };

            todosCollection.InsertOne(todo);
            redis.GetDatabase().HashSet("todo", currentId, description);

            _logger.LogInformation("Added new todo with id {Id} to MongoDB and Redis cache", currentId);

            currentId++;
        }

        public void UpdateTodo(int id, string description)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("description", description);
            todosCollection.UpdateOne(filter, update);

            redis.GetDatabase().HashSet("todo", id, description);

            _logger.LogInformation("Updated todo with id {Id} in MongoDB and Redis cache", id);
        }

        public void DeleteTodo(int id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            todosCollection.DeleteOne(filter);

            redis.GetDatabase().HashDelete("todo", id);

            _logger.LogInformation("Deleted todo with id {Id} from MongoDB and Redis cache", id);
        }
    }
}
