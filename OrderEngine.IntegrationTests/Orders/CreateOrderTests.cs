using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderEngine.Application.Commands.Orders;
using OrderEngine.Application.DTOs;
using OrderEngine.Domain.Enums;
using OrderEngine.Infrastructure.Persistence;
using OrderEngine.IntegrationTests.Setup;
using Xunit;

namespace OrderEngine.IntegrationTests.Orders;

public class CreateOrderTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public CreateOrderTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Should_Create_Order_And_Return_Accepted()
    {
        var client = _factory.CreateClient();
        var command = new CreateOrderCommand(new List<OrderItemDto> { new("Pão de queijo", 2, 5.50m) });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(command)
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var orderId = await response.Content.ReadFromJsonAsync<Guid>();
        orderId.Should().NotBeEmpty();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().ContainSingle().Which.Product.Should().Be("Pão de queijo");
        order.TotalAmount.Should().Be(11.00m);

        // O evento do Outbox é gravado na mesma transação do pedido.
        var outboxMessage = await dbContext.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Type == "OrderCreated" && m.Content.Contains(orderId.ToString()!));

        outboxMessage.Should().NotBeNull();
    }
}
