using bamboo_grpc.Interfaces;
using bamboo_grpc.Repositories;
using bamboo_grpc.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSingleton<ITodosRepository, TodosRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<TodoService>();
// Enable reflection in Debug mode.
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
