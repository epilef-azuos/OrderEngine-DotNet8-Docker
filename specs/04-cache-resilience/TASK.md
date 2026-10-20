# Checklist de Execução - Fase 04

- [x] **T1:** Criar o ramo `feature/04-cache-resilience` a partir da `main`.
- [x] **T2:** Criar `OrderResponseDto` e `GetOrderByIdQuery` no projeto `Application`.
- [x] **T3:** Implementar o `GetOrderByIdQueryHandler`. A lógica deve incluir: verificação no `IDistributedCache`, e caso ocorra um *miss*, adquirir um *lock* no Redis (usando uma chave temporária com *SetNX* ou uma biblioteca de *locking*) antes de ir ao EF Core.
- [x] **T4:** Garantir que a gravação no Redis no momento do *cache miss* usa um `DistributedCacheEntryOptions` com expiração absoluta baseada num tempo base + *Jitter* aleatório.
- [x] **T5:** Implementar o *fallback* no *Handler*: um bloco `try/catch` ao redor da chamada ao Redis para que, se o Redis falhar, vá diretamente ao PostgreSQL (usando `.AsNoTracking()`).
- [x] **T6:** Adicionar o endpoint `GET` no `OrdersController` injetando e despachando a *Query*.
- [x] **T7:** Testar manualmente chamando o GET várias vezes e observando os *logs* da API para confirmar que o `SELECT` só ocorre uma vez.
- [x] **T8:** Fazer o *commit* e *merge* na `main`.