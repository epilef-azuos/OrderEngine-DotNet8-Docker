using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderEngine.Api.Filters;
using OrderEngine.Application.Commands.Orders;
using OrderEngine.Application.Interfaces;
using OrderEngine.Domain.Interfaces;
using OrderEngine.Domain.Interfaces.Repositories;
using OrderEngine.Infrastructure.BackgroundJobs;
using OrderEngine.Infrastructure.Messaging;
using OrderEngine.Infrastructure.Persistence;
using OrderEngine.Infrastructure.Persistence.Queries;
using OrderEngine.Infrastructure.Persistence.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderReadStore, OrderReadStore>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSingleton(new RabbitMqConnectionProvider(builder.Configuration.GetConnectionString("RabbitMq")!));
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxBackgroundService>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// AbortOnConnectFail = false: se o Redis estiver fora no startup, a API sobe e faz fallback para o banco.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddScoped<IdempotencyFilterAttribute>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Permite que o WebApplicationFactory<Program> dos testes de integração acesse a API.
public partial class Program { }
