using bamboo_grpc.Repositories;
using bamboo_grpc.Services;
using bamboo_grpc.MongoDB;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddLogging();

var redisConnect = Environment.GetEnvironmentVariable("REDIS_CONNECT");
if (string.IsNullOrEmpty(redisConnect))
{
    redisConnect = "localhost:6379";
}

builder.Services.AddSingleton<ConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnect)
);

builder.Services.AddSingleton<IMongoDBContext, MongoDBContext>();
builder.Services.AddSingleton<ITodosRepository, TodosRepository>();

var app = builder.Build();

app.MapGrpcService<TodoService>();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet(
    "/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"
);

app.Run();
