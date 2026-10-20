# Plan: Detalhamento Técnico - Fase 05

## 1. Pacotes NuGet Necessários
No projeto `OrderEngine.Infrastructure`:
- `RabbitMQ.Client` (Drivers oficiais do RabbitMQ).

## 2. Estrutura do Domain (`OrderEngine.Domain`)
- `Entities/OutboxMessage.cs`: Id (Guid), Type (string), Content (string/JSON), OccurredOn (DateTime), ProcessedOn (DateTime? nullable), Error (string? nullable).

## 3. Estrutura da Infrastructure (`OrderEngine.Infrastructure`)
- `Persistence/Configurations/OutboxMessageConfiguration.cs`
- `Messaging/RabbitMqConnectionProvider.cs` (Gerenciador Singleton da conexão).
- `Messaging/RabbitMqPublisher.cs` (Publicador na Exchange).
- `BackgroundJobs/OutboxBackgroundService.cs` (Worker que herda de `BackgroundService`).

## 4. Atualização na Application (`OrderEngine.Application`)
- O `CreateOrderCommandHandler` deve criar a entidade `OutboxMessage` contendo o payload serializado do evento (`OrderCreatedEvent`) e adicionar ao `IOrderRepository` (ou `DbContext` diretamente) antes do `SaveChangesAsync()`.