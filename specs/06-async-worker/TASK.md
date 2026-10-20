# Checklist de Execução - Fase 06

- [ ] **T1:** Criar o ramo `feature/06-async-worker` a partir da `main`.
- [ ] **T2:** Instalar `RabbitMQ.Client` e `StackExchange.Redis` no projeto `OrderEngine.Worker`.
- [ ] **T3:** Configurar o `Program.cs` do Worker. Copiar a *Connection String* do Postgres, do Redis e do RabbitMq. Registar o `OrderDbContext`, o Redis (`IConnectionMultiplexer`) e o Worker.
- [ ] **T4:** Criar a classe `OrderProcessingWorker` herdando de `BackgroundService`.
- [ ] **T5:** Implementar a lógica de ligação ao RabbitMQ. O Worker deve declarar uma Fila (ex: `order.processing.queue`) e vinculá-la (*Bind*) à *Exchange* `order.events`.
- [ ] **T6:** Implementar o evento `EventingBasicConsumer.Received`. A lógica DEVE criar um escopo, verificar a idempotência no Redis, adquirir o *Distributed Lock* para os itens, alterar o status do `Order` no Postgres para `Completed`, fazer o `SaveChanges()`, e só então confirmar a mensagem (`BasicAck`).
- [ ] **T7:** Testar correndo a API e o Worker em simultâneo. Criar um pedido e ver o Worker reagir e alterar a base de dados.
- [ ] **T8:** Fazer o *commit* e *merge* na `main`.