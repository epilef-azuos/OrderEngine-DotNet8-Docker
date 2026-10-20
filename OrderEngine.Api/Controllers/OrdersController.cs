using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderEngine.Api.Filters;
using OrderEngine.Application.Commands.Orders;
using OrderEngine.Application.Queries.Orders;

namespace OrderEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ServiceFilter(typeof(IdempotencyFilterAttribute))]
    public async Task<IActionResult> Create([FromHeader(Name = "Idempotency-Key")] string idempotencyKey, [FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(command, cancellationToken);

        return Accepted(orderId);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);

        return order is null ? NotFound() : Ok(order);
    }
}
