using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using bamboo_grpc.Repositories;
using bamboo_grpc.Services;
using bamboo_grpc.MongoDB;
using bamboo_grpc.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddLogging();

var redisConnect = Environment.GetEnvironmentVariable("REDIS_CONNECT");
if (string.IsNullOrEmpty(redisConnect))
{
  redisConnect = "localhost:6379";
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => 
{ 
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtAuthentication.JWT_TOKEN_KEY)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton<ConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnect)
);
builder.Services.AddSingleton<IMongoDBContext, MongoDBContext>();
builder.Services.AddSingleton<ITodosRepository, TodosRepository>();
builder.Services.AddSingleton<IUsersRepository, UsersRepository>();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<AuthenticationService>();
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
