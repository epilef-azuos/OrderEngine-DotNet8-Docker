# Plan: Detalhamento Técnico - Fase 06

## 1. Pacotes NuGet Necessários no Worker
- `RabbitMQ.Client`
- `Microsoft.Extensions.Hosting`
- `StackExchange.Redis`
- Referências para `Application` e `Infrastructure` já existem.

## 2. Estrutura do Worker (`OrderEngine.Worker`)
- `OrderProcessingWorker.cs` (Serviço principal que herda de `BackgroundService`).
- O Worker vai precisar de criar o seu próprio escopo (`IServiceScopeFactory`) para resolver o `OrderDbContext` e gravar as alterações de status.

## 3. Lógica de Concorrência (Redis Lock)
- Injetar o `IConnectionMultiplexer`.
- Para cada item no `OrderCreatedEvent`, executar `LockTakeAsync($"lock:stock:{item.Product}", ...)` antes de "baixar" o stock simulado, seguido de `Task.Delay(500)` para simular I/O, e libertar o *lock* no `finally`.