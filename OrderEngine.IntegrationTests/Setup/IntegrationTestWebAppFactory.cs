using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderEngine.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace OrderEngine.IntegrationTests.Setup;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private readonly RedisContainer _redis;
    private readonly RabbitMqContainer _rabbitMq;

    public IntegrationTestWebAppFactory()
    {
        Environment.SetEnvironmentVariable("DOCKER_API_VERSION", "1.41");

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        _rabbitMq = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _rabbitMq.StartAsync());

        // Banco efémero: cria o schema com as migrations antes dos testes.
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["ConnectionStrings:RabbitMq"] = _rabbitMq.GetConnectionString()
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
