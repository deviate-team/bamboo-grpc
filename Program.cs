using bamboo_grpc.Interfaces;
using bamboo_grpc.Repositories;
using bamboo_grpc.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
builder.WebHost.UseKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS.
    options.ListenLocalhost(builder.Configuration.GetValue<int>("Http2Port"), o => o.Protocols = HttpProtocols.Http2);
});

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
