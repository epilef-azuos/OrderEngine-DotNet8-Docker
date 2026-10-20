# Checklist de Execução - Fase 05

- [x] **T1:** Criar a branch `feature/05-transactional-outbox` a partir da `main`.
- [x] **T2:** Instalar `RabbitMQ.Client` no projeto `Infrastructure`.
- [x] **T3:** Criar a entidade `OutboxMessage` e configurá-la no EF Core. Executar a migração (`dotnet ef migrations add AddOutbox` e `database update`).
- [x] **T4:** Atualizar o `CreateOrderCommandHandler` para serializar o pedido num `OrderCreatedEvent` e inserir como um `OutboxMessage` através do repositório/contexto.
- [x] **T5:** Implementar a infraestrutura do RabbitMQ (`RabbitMqConnectionProvider` e a interface/classe `RabbitMqPublisher`). O publisher deve publicar numa Exchange do tipo `fanout` ou `direct` chamada `order.events`.
- [x] **T6:** Criar o `OutboxBackgroundService` varrendo mensagens com `ProcessedOn == null`, publicando no RabbitMQ e atualizando o `ProcessedOn`. Registar via `builder.Services.AddHostedService<OutboxBackgroundService>()` na API.
- [x] **T7:** Testar criando um pedido na API e verificar se a mensagem aparece na Exchange do painel de controlo do RabbitMQ (`http://localhost:15672`).
- [x] **T8:** Fazer o commit, push e merge na `main`.