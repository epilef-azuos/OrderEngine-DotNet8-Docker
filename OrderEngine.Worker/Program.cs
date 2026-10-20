using Microsoft.EntityFrameworkCore;
using OrderEngine.Infrastructure.Messaging;
using OrderEngine.Infrastructure.Persistence;
using OrderEngine.Worker;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// AbortOnConnectFail = false: o worker sobe mesmo com o Redis fora, e reconecta depois.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddSingleton(new RabbitMqConnectionProvider(builder.Configuration.GetConnectionString("RabbitMq")!));
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
