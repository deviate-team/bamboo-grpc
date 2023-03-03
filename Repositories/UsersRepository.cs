using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using Newtonsoft.Json;
using bamboo_grpc.Models;
using bamboo_grpc.Interfaces;

namespace bamboo_grpc.Repositories;

public class UsersRepository : IUsersRepository
{
  private readonly ILogger<UsersRepository> _logger;
  private readonly IMongoCollection<UserModel> _users;
  private readonly ConnectionMultiplexer _redisContext;

  public UsersRepository(
    ILoggerFactory loggerFactory,
    IMongoDBContext mongoDBContext,
    ConnectionMultiplexer redisContext
  )
  {
    this._logger = loggerFactory.CreateLogger<UsersRepository>();
    this._users = mongoDBContext.GetCollection<UserModel>("users");
    this._redisContext = redisContext;
  }

  public async Task<IEnumerable<UserModel>> GetUsers()
  {
    try
    {
      string cacheKey = "users:all";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "[]")
      {
        _logger.LogInformation("Get Data from cache..");
        return JsonConvert.DeserializeObject<IEnumerable<UserModel>>(cacheData);
      }

      var users = await _users.Find(_ => true).ToListAsync();
      _logger.LogInformation("Get Data from Mongo..");
      if (users != null)
      {
        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(users), TimeSpan.FromMinutes(15));
      }

      return users;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get users: {ex}");
      throw;
    }
  }

  public async Task<UserModel> GetUserById(string id)
  {
    try
    {
      string cacheKey = $"users:{id}";
      string cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "{}")
      {
        _logger.LogInformation("Get Data from cache..");
        return JsonConvert.DeserializeObject<UserModel>(cacheData);
      }

      var user = await _users.Find(_ => _.Id == id).FirstOrDefaultAsync();
      _logger.LogInformation("Get Data from Mongo..");
      if (user != null)
      {
        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(15));
      }

      return user;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get user: {ex}");
      throw;
    }
  }

  public async Task<UserModel> GetUserByUsername(string username)
  {
    try
    {
      var cacheKey = $"users:{username}";
      var cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "{}")
      {
        _logger.LogInformation("Get Data from cache..");
        return JsonConvert.DeserializeObject<UserModel>(cacheData);
      }

      var user = await _users.Find(_ => _.Username == username).FirstOrDefaultAsync();
      _logger.LogInformation("Get Data from Mongo..");
      if (user != null)
      {
        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(15));
      }
      return user;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get user: {ex}");
      throw;
    }
  }

  public async Task<UserModel> GetUserByEmail(string email)
  {
    try
    {
      var cacheKey = $"users:{email}";
      var cacheData = await _redisContext.GetDatabase().StringGetAsync(cacheKey);
      if (!string.IsNullOrEmpty(cacheData) && cacheData != "{}")
      {
        _logger.LogInformation("Get Data from cache..");
        return JsonConvert.DeserializeObject<UserModel>(cacheData);
      }

      var user = await _users.Find(_ => _.Email == email).FirstOrDefaultAsync();
      _logger.LogInformation("Get Data from Mongo..");
      if (user != null)
      {
        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKey, JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(15));
      }
      return user;
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to get user: {ex}");
      throw;
    }
  }

  public async Task InsertUser(string username, string password, string email, string role)
  {
    try
    {
      var user = new UserModel
      {
        Id = ObjectId.GenerateNewId().ToString(),
        Username = username,
        Password = password,
        Email = email,
        Role = role
      };
      await _users.InsertOneAsync(user);
      _logger.LogInformation("Inserted user to Mongo..");

      var cacheKeyAll = "users:all";
      var cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);

      if (!string.IsNullOrEmpty(cacheDataAll))
      {
        var users = JsonConvert.DeserializeObject<IEnumerable<UserModel>>(cacheDataAll);
        users = users.Append(user);

        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKeyAll, JsonConvert.SerializeObject(users), TimeSpan.FromMinutes(15));
      }
      var cachekey = $"users:{user.Id}";
      var cacheData = await _redisContext.GetDatabase().StringGetAsync(cachekey);
      if (!string.IsNullOrEmpty(cacheData))
      {
        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cachekey, JsonConvert.SerializeObject(user), TimeSpan.FromMinutes(15));
      }
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to insert user: {ex}");
      throw;
    }
  }

  public async Task UpdateUser(string id, string username, string password, string email, string role)
  {
    try
    {
      var existingUser = await _users.Find(_ => _.Id == id).FirstOrDefaultAsync();
      if (existingUser == null)
      {
        throw new Exception("User not found");
      }
      existingUser.Username = username;
      existingUser.Password = password;
      existingUser.Email = email;
      await _users.ReplaceOneAsync(_ => _.Id == id, existingUser);
      _logger.LogInformation("Updated user in Mongo..");

      var cachekey = $"users:{id}";
      await _redisContext.GetDatabase().StringSetAsync(cachekey, JsonConvert.SerializeObject(existingUser), TimeSpan.FromMinutes(15));

      var cacheKeyAll = "users:all";
      var cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);

      if (!string.IsNullOrEmpty(cacheDataAll))
      {
        var users = JsonConvert.DeserializeObject<IEnumerable<UserModel>>(cacheDataAll);
        var updatedUsers = users.Select(user =>
        {
          if (user.Id == id)
          {
            user.Username = username;
            user.Password = password;
            user.Email = email;
          }
          return user;
        });
        _logger.LogInformation("Cached data to Redis..");
        await _redisContext.GetDatabase().StringSetAsync(cacheKeyAll, JsonConvert.SerializeObject(updatedUsers), TimeSpan.FromMinutes(15));
      }
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to update user: {ex}");
      throw;
    }
  }

  public async Task DeleteUserById(string id)
  {
    try
    {
      var result = await _users.DeleteOneAsync(_ => _.Id == id);
      _logger.LogInformation("Deleted user from Mongo..");
      if (result.DeletedCount == 1)
      {
        string cachekey = $"users:{id}";
        await _redisContext.GetDatabase().KeyDeleteAsync(cachekey);
        _logger.LogInformation("Deleted user from Redis..");

        var cacheKeyAll = "users:all";
        var cacheDataAll = await _redisContext.GetDatabase().StringGetAsync(cacheKeyAll);
        if (!string.IsNullOrEmpty(cacheDataAll) && cacheDataAll != "[]")
        {
          var users = JsonConvert.DeserializeObject<IEnumerable<UserModel>>(cacheDataAll);
          var updatedUsers = users.Where(user => user.Id != id);
          _logger.LogInformation("delete data in Redis..");
          await _redisContext.GetDatabase().StringSetAsync(cacheKeyAll, JsonConvert.SerializeObject(updatedUsers), TimeSpan.FromMinutes(15));
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError($"Failed to delete user: {ex}");
      throw;
    }
  }
}