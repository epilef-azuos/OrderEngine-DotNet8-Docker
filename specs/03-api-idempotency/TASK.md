# Checklist de Execução - Fase 03

- [x] **T1:** Criar a branch `feature/03-api-idempotency` a partir da `main`.
- [x] **T2:** Instalar os pacotes `MediatR` no projeto `Application` e `Microsoft.Extensions.Caching.StackExchangeRedis` na `Api`.
- [x] **T3:** Criar as classes `CreateOrderCommand`, `OrderItemDto` e `CreateOrderCommandHandler` no projeto `Application`. O Handler deve injetar o `IOrderRepository`, instanciar o `Order`, adicionar os itens e salvar no banco.
- [x] **T4:** Adicionar a Connection String do Redis (`"Redis": "localhost:6379"`) no `appsettings.Development.json` e registrar os serviços (Redis e MediatR) no `Program.cs`.
- [x] **T5:** Implementar o `IdempotencyFilterAttribute` na camada `Api`, utilizando `IDistributedCache` para validar o header `Idempotency-Key` antes da execução e salvar a resposta de sucesso após a execução.
- [x] **T6:** Criar o `OrdersController` anotado com `[ServiceFilter(typeof(IdempotencyFilterAttribute))]` (ou usar o atributo diretamente se gerenciar as injeções localmente) e implementar o método `POST`.
- [x] **T7:** Rodar a aplicação (`dotnet run --project OrderEngine.Api/OrderEngine.Api.csproj`) e disparar duas requisições idênticas via Swagger ou Postman/cURL para validar o Redis bloqueando a duplicação.
- [x] **T8:** Fazer o commit, push e merge na `main`.