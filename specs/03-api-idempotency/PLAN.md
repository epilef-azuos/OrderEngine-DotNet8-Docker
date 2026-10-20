# Plan: Detalhamento Técnico - Fase 03

## 1. Pacotes NuGet Necessários
No projeto `OrderEngine.Application`:
- `MediatR`

No projeto `OrderEngine.Api`:
- `Microsoft.Extensions.Caching.StackExchangeRedis` (Para comunicação direta e eficiente com o Redis via IDistributedCache).

## 2. Estrutura da Camada Application (`OrderEngine.Application`)
- `Commands/Orders/CreateOrderCommand.cs` (Record contendo os dados do request).
- `Commands/Orders/CreateOrderCommandHandler.cs` (Implementa `IRequestHandler<CreateOrderCommand, Guid>`).
- `DTOs/OrderItemDto.cs`

## 3. Estrutura da Camada Api (`OrderEngine.Api`)
- `Controllers/OrdersController.cs`
- `Filters/IdempotencyFilterAttribute.cs` (ActionFilterAttribute que intercepta a requisição, checa o Redis, e caso não exista, deixa prosseguir, interceptando a resposta na volta para "cachear" o resultado).

## 4. Configuração da Injeção de Dependência (`Program.cs`)
- Registrar o MediatR: `builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));`
- Registrar o Redis: `builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = builder.Configuration.GetConnectionString("Redis"); });`