# Plan: Detalhamento Técnico - Fase 04

## 1. Padrões de Cache a Implementar
- **Cache-Aside:** Lê do Redis; se falhar (miss), lê da base de dados e guarda no Redis.
- **Cache Stampede Prevention:**
  - Jitter: `TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(Random.Shared.Next(0, 60))`.
  - Distributed Lock: Utilizar um semáforo ou trava distribuída para garantir o *Singleflight* na recarga.

## 2. Estrutura da Camada Application (`OrderEngine.Application`)
- `Queries/Orders/GetOrderByIdQuery.cs`
- `Queries/Orders/GetOrderByIdQueryHandler.cs`
- *Nota:* Como a *Query* não altera estado, ela pode contornar o repositório e injetar o `IOrderDbContext` diretamente para efetuar projeções (AsNoTracking) otimizadas (opcional, mas recomendado no CQRS).

## 3. Estrutura de Infraestrutura de Cache
- Criar um serviço encapsulado `ICacheService` (na camada `Infrastructure`) ou adicionar a lógica de travamento diretamente no *Handler*, usando `IDistributedCache`.