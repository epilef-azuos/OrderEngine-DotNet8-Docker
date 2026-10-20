# OrderEngine: Análise Arquitetural Aprofundada

Documento de estudo sobre o estado da solução `OrderEngine.sln` até a Fase 07. Descreve a estrutura, os pacotes, as decisões de design, os fluxos de concorrência e as limitações conhecidas. As limitações estão listadas explicitamente porque são o que diferencia um entendimento superficial de um entendimento de nível sênior.

---

## 1. Mapeamento de Projetos

A solução tem seis projetos. Os cinco de produção seguem Clean Architecture: as dependências apontam sempre para dentro, em direção ao Domínio.

```
                 ┌──────────────┐        ┌──────────────┐
                 │   Api        │        │   Worker     │
                 │ (ASP.NET)    │        │ (Generic Host)│
                 └──┬───────┬───┘        └──┬───────┬───┘
                    │       │               │       │
                    ▼       ▼               ▼       ▼
             ┌────────────┐ ┌──────────────────┐
             │ Application│◄┤ Infrastructure   │
             └─────┬──────┘ └──────────────────┘
                   │
                   ▼
             ┌────────────┐
             │   Domain   │
             └────────────┘
```

### Referências entre projetos (verificadas nos `.csproj`)

| Projeto | Referencia |
|---|---|
| `OrderEngine.Domain` | nenhum projeto |
| `OrderEngine.Application` | `Domain` |
| `OrderEngine.Infrastructure` | `Application` (e, transitivamente, `Domain`) |
| `OrderEngine.Api` | `Application` e `Infrastructure` |
| `OrderEngine.Worker` | `Application` e `Infrastructure` |
| `OrderEngine.IntegrationTests` | `Api` e `Infrastructure` |

### Responsabilidade de cada projeto

**`OrderEngine.Domain`** (regras de negócio puras)
- Entidades: `Order`, `OrderItem`, `Payment`, `OutboxMessage`.
- Enums: `OrderStatus` (`Pending`, `Processing`, `Completed`, `Failed`) e `PaymentStatus`.
- Contratos de persistência: `IOrderRepository`, `IPaymentRepository`, `IOutboxRepository` e `IUnitOfWork`.
- Não depende de nenhum pacote. Essa ausência é uma garantia arquitetural: o domínio não conhece EF Core, Redis, RabbitMQ ou ASP.NET.

**`OrderEngine.Application`** (casos de uso, orquestração via CQRS)
- Commands e handlers MediatR (`CreateOrderCommand`).
- Queries e handlers MediatR (`GetOrderByIdQuery`), com o padrão cache-aside e o lock de recarga.
- DTOs de transferência (`OrderItemDto`, `OrderResponseDto`, `OrderItemResponseDto`).
- Evento de integração `OrderCreatedEvent`, que é serializado no Outbox.
- Porta de leitura `IOrderReadStore`, que isola a consulta do EF Core.

**`OrderEngine.Infrastructure`** (adaptadores de tecnologia)
- `OrderDbContext` e configurações Fluent API (`IEntityTypeConfiguration<T>`), além das migrations `InitialCreate` e `AddOutbox`.
- Implementações de repositórios, `UnitOfWork` e `OrderReadStore` (com `AsNoTracking()`).
- Mensageria: `RabbitMqConnectionProvider` (singleton) e `RabbitMqPublisher`.
- `OutboxBackgroundService`, o publicador em background do padrão Transactional Outbox.

**`OrderEngine.Api`** (borda HTTP, composição da aplicação)
- `OrdersController`: anêmico, apenas recebe a requisição, despacha via MediatR e devolve o status HTTP.
- `IdempotencyFilterAttribute`: filtro transversal que implementa a idempotência de criação.
- `Program.cs`: é o *composition root*, o único lugar onde as implementações concretas são registradas.

**`OrderEngine.Worker`** (consumidor assíncrono)
- `OrderProcessingWorker`: consome a fila `order.processing.queue`, processa o pedido e confirma a mensagem.
- Também é composition root do seu próprio processo, com o próprio `DbContext`, Redis e RabbitMQ.

**`OrderEngine.IntegrationTests`** (qualidade)
- Testes de integração contra PostgreSQL, Redis e RabbitMQ reais, sobem via Testcontainers.

---

## 2. Inventário de Pacotes NuGet

Versões conforme os `.csproj` atuais. Todos os pacotes da linha `9.0.x` seguem o runtime `net9.0`. Pacotes da linha 10 não foram usados justamente porque não são compatíveis com o target.

### `OrderEngine.Domain`
Sem pacotes externos. Essa é uma decisão deliberada, não uma omissão.

### `OrderEngine.Application`

| Pacote | Versão | Uso no sistema |
|---|---|---|
| **MediatR** | 12.5.0 | Implementa o despacho CQRS. A API envia `CreateOrderCommand` e `GetOrderByIdQuery` sem conhecer o handler; o controller fica desacoplado da lógica. A versão 12.5 é a última com licença Apache-2.0 gratuita: a partir da 13 o uso comercial exige licença paga. |
| **StackExchange.Redis** | 2.7.27 | Cliente Redis de baixo nível. Usado no handler de leitura para `LockTakeAsync`/`LockReleaseAsync`, a primitiva de lock distribuído. O `IDistributedCache` não expõe `SET NX`, então o cliente direto é necessário para a trava. |
| **Microsoft.Extensions.Caching.Abstractions** | 9.0.21 | Fornece a interface `IDistributedCache`. A Application depende da abstração, não do Redis: a implementação concreta é trocável. |
| **Microsoft.Extensions.Logging.Abstractions** | 9.0.21 | Fornece `ILogger<T>`. A Application registra avisos de fallback sem depender de um provedor de log específico. |

### `OrderEngine.Infrastructure`

| Pacote | Versão | Uso no sistema |
|---|---|---|
| **Microsoft.EntityFrameworkCore** | 9.0.21 | ORM. Mapeia as entidades de domínio e gera o SQL. `AsNoTracking()` é usado nas leituras para evitar o overhead do Change Tracker. |
| **Microsoft.EntityFrameworkCore.Design** | 9.0.21 | Ferramentas de design-time (`dotnet ef`): geram migrations e consultam o modelo. `PrivateAssets=all`, então não vaza para os projetos consumidores. |
| **Microsoft.EntityFrameworkCore.Relational** | 9.0.21 | Base relacional do EF Core. Referenciado diretamente para fixar a versão, pois o Npgsql puxa uma versão mínima mais antiga e o SDK 9 trata o downgrade como erro (`NU1605`). |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 9.0.4 | Provedor EF Core para PostgreSQL. Traduz LINQ para SQL do Postgres e mapeia tipos como `timestamptz`. |
| **RabbitMQ.Client** | 7.2.2 | Cliente oficial do RabbitMQ. A versão 7 é totalmente assíncrona (`IChannel`, `AsyncEventingBasicConsumer`). Usado para declarar exchanges e filas, publicar mensagens e consumir com ack manual. |
| **Microsoft.Extensions.Hosting.Abstractions** | 9.0.21 | Fornece a classe base `BackgroundService`, usada pelo `OutboxBackgroundService`. Está só na Infrastructure porque é a única que precisa dela. |

### `OrderEngine.Api`

| Pacote | Versão | Uso no sistema |
|---|---|---|
| **Microsoft.AspNetCore.OpenApi** | 9.0.20 | Gera o documento OpenAPI nativo do .NET 9 (`/openapi/v1.json`). |
| **Microsoft.EntityFrameworkCore.Design** | 9.0.21 | Permite que o `dotnet ef` use a API como startup project. |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | 9.0.21 | Implementa `IDistributedCache` sobre o Redis. Usado pelo `IdempotencyFilterAttribute` para guardar respostas. |
| **Swashbuckle.AspNetCore** | 9.0.6 | Swagger UI (`/swagger`). Adicionado porque o `AddOpenApi` sozinho só expõe o JSON, sem interface para testar. |

### `OrderEngine.Worker`

| Pacote | Versão | Uso no sistema |
|---|---|---|
| **Microsoft.Extensions.Hosting** | 9.0.20 | Host genérico e `Host.CreateApplicationBuilder`, que hospedam o `OrderProcessingWorker` como serviço. |
| **Microsoft.EntityFrameworkCore** / **Relational** / **Npgsql.EntityFrameworkCore.PostgreSQL** | 9.0.21 / 9.0.21 / 9.0.4 | O Worker atualiza o status do pedido no banco, então precisa do mesmo stack de persistência da API. |
| **RabbitMQ.Client** | 7.2.2 | Consome `order.processing.queue`, com ack/nack manual e DLX. |
| **StackExchange.Redis** | 2.7.27 | Idempotência (`worker:processed:{id}`), lock do pedido e locks de estoque. |

### `OrderEngine.IntegrationTests`

| Pacote | Versão | Uso no sistema |
|---|---|---|
| **Microsoft.NET.Test.Sdk** / **xunit** / **xunit.runner.visualstudio** | 17.12.0 / 2.9.2 / 2.8.2 | Plataforma de testes. O xUnit cria uma instância da classe de teste por método e oferece `IClassFixture` e `IAsyncLifetime`, usados para compartilhar a factory e os contêineres. |
| **Microsoft.AspNetCore.Mvc.Testing** | 9.0.21 | `WebApplicationFactory<Program>`: sobe a API em memória, com `TestServer`, sem abrir porta real. |
| **Testcontainers.PostgreSql** / **Redis** / **RabbitMq** | 4.16.0 | Sobem contêineres Docker efêmeros para cada execução. Garante que o teste fala com infraestrutura real, sem mocks, como exige a constituição da Fase 07. |
| **FluentAssertions** | 7.2.2 | Asserções legíveis. A versão 7 é a última com licença Apache-2.0 gratuita. |
| **coverlet.collector** | 6.0.2 | Coleta de cobertura de código, gerada pelo template. |

### Pacotes que **não** estão no projeto, e por quê

- **Polly** (resiliência: retry, circuit breaker, timeout): não foi adotado. A resiliência atual é feita manualmente, com try/catch e fallback explícito nos pontos de falha (Redis, RabbitMQ). Em um sistema maior, Polly seria a escolha natural para envolver chamadas ao RabbitMQ e ao Redis com retry exponencial e circuit breaker, em vez de repetir o padrão à mão.
- **MassTransit / NServiceBus**: abstrações de mensageria de alto nível. Foram evitadas para manter o controle explícito de ack, nack, DLX e exchanges, que é justamente o que se quer estudar.

---

## 3. Decisões de Design (ADRs Resumidos)

### ADR-01: Clean Architecture com dependências apontando para o Domínio

**Contexto:** a solução precisa testar as regras de negócio sem infraestrutura e trocar tecnologias sem reescrever o domínio.

**Decisão:** o Domínio não tem dependências. A Application depende só do Domínio. A Infrastructure implementa as portas definidas nas camadas internas.

**Consequência:** `Order` e `Payment` não têm atributos do EF Core. O mapeamento fica em `IEntityTypeConfiguration<T>` na Infrastructure. Trocar o Postgres por outro banco exigiria mudar apenas a Infrastructure.

**Limitação real:** a Application referencia `StackExchange.Redis` diretamente, porque o handler de leitura usa `IConnectionMultiplexer` para a trava. Isso vaza um detalhe de infraestrutura para a camada de aplicação. A correção seria uma porta `IDistributedLock` na Application, implementada na Infrastructure.

### ADR-02: CQRS com MediatR

**Contexto:** a escrita (criar pedido) tem regras e efeitos colaterais; a leitura (consultar pedido) precisa ser rápida e sem efeitos.

**Decisão:** comandos e consultas são objetos separados, despachados pelo MediatR. A consulta retorna DTOs, nunca entidades de domínio.

**Consequência:** o controller não sabe qual handler executa a lógica. A leitura pode usar um caminho otimizado (cache + `AsNoTracking()` + projeção) sem contaminar o modelo de escrita.

**Limitação real:** a leitura ainda passa pela mesma infraestrutura de persistência (`OrderDbContext`). Um modelo de leitura separado, por exemplo uma tabela desnormalizada, seria o próximo passo do CQRS completo.

### ADR-03: Idempotência de criação via filtro transversal (Fase 03)

**Contexto:** uma requisição `POST` pode ser reenviada pelo cliente após um timeout de rede, gerando pedidos duplicados.

**Decisão:** o `IdempotencyFilterAttribute` (um `IAsyncActionFilter`) lê o header `Idempotency-Key`:
1. Sem header, responde 400.
2. Com a chave já guardada no Redis (`idempotency:{chave}`), devolve a resposta original (status e corpo) sem executar a action.
3. Caso contrário, executa a action e, se o resultado for 2xx, guarda status e corpo por 24 horas.

**Por que um filtro e não um middleware ou o handler:** a regra de idempotência é uma preocupação transversal de borda HTTP. Colocá-la no handler violaria a separação do MediatR; em um middleware, o filtro teria acesso ao resultado tipado da action.

**Limitação real (condição de corrida):** a verificação e a gravação não são atômicas. Duas requisições simultâneas com a mesma chave podem ambas passar pela checagem e criar dois pedidos. A correção é um `SET key value NX` atômico no Redis, reservando a chave antes de executar a action, o que o `IDistributedCache` não oferece diretamente.

### ADR-04: Transactional Outbox (Fase 05)

**Contexto:** criar o pedido no banco e publicar o evento no RabbitMQ são duas operações em sistemas diferentes. Se uma falhar depois da outra, o sistema fica inconsistente (pedido sem evento, ou evento sem pedido). Não existe transação distribuída barata entre PostgreSQL e RabbitMQ.

**Decisão:**
- O handler de criação grava o `Order` e um `OutboxMessage` com o `OrderCreatedEvent` serializado em JSON, em **um único `SaveChangesAsync`**, na mesma transação do PostgreSQL.
- Um `BackgroundService` (`OutboxBackgroundService`) consulta periodicamente as mensagens com `ProcessedOn IS NULL`, publica no RabbitMQ e marca `ProcessedOn`.

**Por que isso funciona:** a atomicidade da escrita fica garantida pelo banco. A publicação é reconciliada depois, por um processo que pode falhar e recomeçar sem perder eventos.

**Garantia de entrega:** *at-least-once*. Se o worker cair depois de publicar e antes de salvar `ProcessedOn`, a mensagem é publicada de novo. Por isso o consumidor precisa ser idempotente (ADR-06).

**Decisões de implementação relevantes:**
- O `IUnitOfWork` foi introduzido porque o `AddAsync` do repositório fazia `SaveChanges` sozinho, o que quebraria a atomicidade. Agora só o handler efetiva a transação.
- `OccurredOn` exige `DateTimeKind.Utc`, e as colunas são `timestamptz`. O Npgsql rejeita `DateTime` com `Kind` diferente de UTC em `timestamp with time zone`.
- O `SaveChanges` do worker é em lote, no fim de cada ciclo de 20 mensagens, não por mensagem.

**Limitações reais:**
- Sem backoff: se o broker estiver fora, todas as mensagens do lote falham a cada 5 segundos e recebem a coluna `Error` atualizada.
- Sem limite de tentativas: uma mensagem envenenada (que sempre falha ao publicar) fica para sempre na tabela.
- O `RabbitMqPublisher` cria um canal por mensagem. É simples e seguro, mas caro em alto volume. Um canal dedicado ou publicação em lote com *publisher confirms* seria a otimização.

### ADR-05: Cache-aside com proteção contra Cache Stampede (Fase 04)

**Contexto:** quando uma chave quente expira, milhares de requisições simultâneas podem consultar o banco ao mesmo tempo (*thundering herd*).

**Decisão, nesta ordem:**
1. Lê o cache `order:{id}` dentro de try/catch. Se o Redis estiver fora, cai para o banco.
2. Em miss, tenta `LockTakeAsync` em `lock:order-processing:{id}`, com expiração de 5 segundos.
3. Quem não obtém a trava espera (polling de 100 ms por até 5 segundos), lendo o cache a cada iteração.
4. Quem obtém a trava faz o **double-check** no cache antes de ir ao banco. Outra requisição pode ter preenchido o cache enquanto esperávamos.
5. Consulta o banco com `AsNoTracking()` e grava o resultado com TTL de **5 minutos mais jitter de 0 a 59 segundos**, para que chaves gravadas juntas não expirem no mesmo instante.
6. Libera a trava no `finally`, com `LockReleaseAsync`, usando o valor como prova de posse.

**Por que o valor da trava é `MachineName:Guid`:** `LockReleaseAsync` libera a chave só se o valor bater. Sem o GUID, uma requisição com trava expirada poderia liberar a trava de outra.

**Decisões de falha:**
- Redis indisponível: a leitura segue direto para o banco, sem cache e sem trava.
- Espera esgotada (5 segundos sem conseguir a trava nem o cache): lê do banco sem trava e registra um warning. Assim nenhuma requisição fica presa indefinidamente.

**Limitações reais:**
- **Sem fencing token:** se o processo que segura a trava demorar mais que os 5 segundos de expiração, outro processo pode entrar na seção crítica ao mesmo tempo. Um *fencing token* monotônico, verificado pelo recurso protegido, seria a garantia forte.
- **Pedidos inexistentes não são cacheados**, então um ID inválido repetido sempre vai ao banco. Um *negative cache* curto resolveria.
- **Cache de leitura pode ficar desatualizado** até o TTL, a não ser que alguém o invalide. O worker invalida `order:{id}` após concluir o pedido (ADR-07).

### ADR-06: Consumidor idempotente com lock e ack manual (Fases 06 e correções)

**Contexto:** o RabbitMQ entrega *pelo menos uma vez*. O mesmo pedido pode chegar duas vezes, e duas instâncias do worker podem tentar processá-lo ao mesmo tempo.

**Decisão, em camadas, por mensagem:**

1. **Ack manual:** `autoAck: false`. O ack só é enviado depois que o banco confirma e a idempotência é gravada.
2. **Verificação rápida no Redis:** se `worker:processed:{orderId}` existe (TTL de 7 dias), a mensagem é duplicata e recebe ack imediato.
3. **Trava do pedido:** `lock:order-processing:{orderId}` (10 segundos). Sem a trava, a mensagem volta para a fila com requeue.
4. **Escopo próprio:** `IServiceScopeFactory` cria um escopo por mensagem, porque o worker é singleton e o `DbContext` é scoped.
5. **Double-check no banco:** se o pedido já estiver `Completed` ou `Failed`, a mensagem é ignorada. Essa checagem cobre o caso em que a chave do Redis foi perdida, e é a verdadeira garantia de idempotência.
6. **Transições de domínio:** `Pending → Processing` (salvo), simulação de pagamento, `Processing → Completed` (salvo). O domínio impede transições inválidas.
7. **Locks de estoque:** para cada produto distinto, `lock:stock:{Product}` (10 segundos), 500 ms de simulação de baixa e liberação imediata. Um lock por vez, então nunca se segura um produto enquanto se espera outro: isso evita *deadlock* circular entre pedidos.
8. **Confirmação:** grava `worker:processed:{orderId}` e faz ack.

**Por que a idempotência de verdade está no banco, e a do Redis é só otimização:** o Redis evita trabalho repetido, mas pode ser perdido (reinício, evicção, falha). O estado `Completed` no PostgreSQL é a fonte da verdade, que sobrevive a qualquer falha de cache.

**Tratamento de falhas:**
- Redis indisponível: nack com requeue, para não perder o pedido.
- Trava não obtida: nack com requeue, após um atraso de 1 segundo.
- Falha não controlada: nack **sem** requeue, para não criar loop infinito. A mensagem vai para a DLQ (ADR-08).

**Limitações reais:**
- **Sem fencing token** (mesma observação do ADR-05). Se a simulação demorar mais que o TTL da trava, o pedido pode ser processado duas vezes em paralelo. O double-check no banco limita o dano, mas não impede a concorrência.
- **Falha transitória do banco também vai para a DLQ.** Um erro de conexão momentâneo descarta a mensagem em vez de tentar de novo. Uma política de retry com atraso, antes do nack final, resolveria.

### ADR-07: Invalidação explícita do cache de leitura

**Decisão:** após gravar `Completed` no banco, o worker chama `KeyDeleteAsync("order:{orderId}")`. O próximo GET busca a versão fresca.

**Por que é "best effort":** se a invalidação falhar, o banco já está correto e o cache expira sozinho em até 5 minutos. Um erro aqui não deve reprocessar o pedido.

**Acoplamento a documentar:** o worker conhece a string da chave do cache da API. Se a API mudar o formato da chave ou adicionar um `InstanceName` ao `AddStackExchangeRedisCache`, a invalidação para de funcionar silenciosamente. Uma solução mais robusta seria um evento `OrderCompleted` publicado no Outbox, consumido pela própria API para invalidar seu cache.

### ADR-08: Dead Letter Exchange para mensagens rejeitadas

**Decisão:** a fila `order.processing.queue` é declarada com `x-dead-letter-exchange = order.dlx` e `x-dead-letter-routing-key = order.dlq`. A exchange `order.dlx` (direct) roteia para a fila `order.dlq`.

**Ponto técnico importante:** os argumentos de DLX pertencem à **declaração da fila**, não ao bind. Como a fila já existia sem esses argumentos, o RabbitMQ rejeita a redeclaração com `PRECONDITION_FAILED`. A solução foi apagar a fila vazia antes de subir a nova versão, o que é um procedimento de migração de infraestrutura a registrar.

**Limitação real:** a DLQ não tem consumidor nem alerta. Mensagens lá ficam esquecidas até alguém olhar o painel do RabbitMQ. Em produção, a DLQ precisaria de monitoramento e de um mecanismo de reprocessamento.

### ADR-09: Testes contra infraestrutura real (Fase 07)

**Decisão:** nenhum mock de infraestrutura nos testes de integração. `IntegrationTestWebAppFactory` sobe PostgreSQL, Redis e RabbitMQ em contêineres, aplica as migrations e substitui as connection strings em memória, via `ConfigureAppConfiguration`.

**Por que o `public partial class Program`:** a API usa top-level statements, então a classe `Program` é gerada como `internal`. A declaração parcial pública expõe o tipo para o `WebApplicationFactory<Program>`.

**Limitação real:** um contêiner por classe de teste, não por teste. A constituição pede isolamento por ciclo de teste. Com um único teste funciona, mas testes que mutam o mesmo banco podem interferir uns nos outros.

---

## 4. Fluxo End-to-End (do HTTP ao estado `Completed`)

```
Cliente ──POST /api/orders (Idempotency-Key)──► IdempotencyFilter
                                                  │ miss
                                                  ▼
                                    OrdersController.Create
                                                  │ MediatR.Send
                                                  ▼
                              CreateOrderCommandHandler
                    ┌─────────────────────────────┴──────────────────────┐
                    │ 1. new Order(id) + AddOrderItem (regras no domínio) │
                    │ 2. IOrderRepository.AddAsync (sem SaveChanges)      │
                    │ 3. IOutboxRepository.AddAsync (OrderCreatedEvent)   │
                    │ 4. IUnitOfWork.SaveChangesAsync  ← 1 transação      │
                    └─────────────────────────────┬──────────────────────┘
                                                  │ 202 Accepted + Guid
                                                  ▼
PostgreSQL ──► OutboxMessages (ProcessedOn IS NULL)
                                                  │ OutboxBackgroundService (a cada 5 s, lote de 20)
                                                  ▼
                              RabbitMqPublisher → exchange order.events (fanout)
                                                  │
                                                  ▼
                              order.processing.queue (prefetch 1, ack manual)
                                                  │ OrderProcessingWorker
                                                  ▼
            Redis: idempotência · lock:order-processing · lock:stock:* ──► PostgreSQL: Pending→Processing→Completed
                                                  │
                                                  ▼
                              Redis: DEL order:{id} · SET worker:processed:{id} (7 d) · ack
```

---

## 5. Limitações Conhecidas (resumo para revisão)

| # | Limitação | Impacto | Correção sugerida |
|---|---|---|---|
| 1 | Application referencia StackExchange.Redis | Vazamento de infraestrutura para a camada de aplicação | Porta `IDistributedLock` na Application |
| 2 | Idempotência da API tem condição de corrida | Dois pedidos com a mesma chave em paralelo | `SET NX` atômico reservando a chave |
| 3 | Locks sem fencing token | Processamento paralelo se a trava expirar | Token monotônico verificado no recurso |
| 4 | Falhas transitórias do banco vão para a DLQ | Pedido pode ficar parado em `Pending` | Retry com backoff antes do nack final |
| 5 | Outbox sem backoff nem limite de tentativas | Mensagem envenenada fica para sempre | Contador de tentativas e cap no `Error` |
| 6 | `IPaymentRepository` sem implementação | Contrato definido, mas inutilizado | Implementar na Infrastructure quando a fase de pagamento começar |
| 7 | Sem consumidor nem alerta na DLQ | Falhas somem silenciosamente | Monitoramento e reprocessamento |
| 8 | Dois clientes Redis (cache via `AddStackExchangeRedisCache` e lock via `IConnectionMultiplexer`) | Duas conexões ao mesmo servidor | A extensão padrão não aceita um multiplexer existente; a alternativa é implementar `IDistributedCache` sobre o próprio `IConnectionMultiplexer` |
| 9 | Credenciais em `appsettings.Development.json` | Senhas versionadas no Git | Migrar para User Secrets ou variáveis de ambiente |
| 10 | Teste de integração com um contêiner por classe | Interferência entre testes que mutam o mesmo banco | Um banco por teste, via schema ou `Respawn` |

---

## 6. Comandos Essenciais

### Infraestrutura (Docker Compose)

```bash
# Sobe PostgreSQL 16, Redis 7 e RabbitMQ 3 com painel de gerenciamento
docker-compose up -d

# Acompanha o status e os healthchecks
docker-compose ps

# Para os contêineres, mantendo os volumes (dados preservados)
docker-compose down

# Para e APAGA os volumes (reset completo do banco)
docker-compose down -v

# Logs de um serviço
docker-compose logs -f postgres
```

### Acesso direto aos serviços

```bash
# Shell do PostgreSQL
docker exec -it orderengine-postgres psql -U postgres -d orderenginedb

# CLI do Redis
docker exec -it orderengine-redis redis-cli

# Inspeciona uma chave de idempotência ou de cache
docker exec -it orderengine-redis redis-cli GET "worker:processed:<guid>"
docker exec -it orderengine-redis redis-cli TTL "order:<guid>"

# Lista as locks em uso
docker exec -it orderengine-redis redis-cli KEYS "lock:*"
```

- **Painel do RabbitMQ:** `http://localhost:15672` (usuário e senha `guest`).
- **Swagger da API:** `http://localhost:5119/swagger` (perfil `http`, em Development).

### Entity Framework Core (migrations)

Os comandos sempre apontam para o projeto que contém o `DbContext` (Infrastructure) e para o projeto que tem a connection string (Api).

```bash
# Gera uma nova migration
dotnet ef migrations add <NomeDaMigration> \
  --project OrderEngine.Infrastructure/OrderEngine.Infrastructure.csproj \
  --startup-project OrderEngine.Api/OrderEngine.Api.csproj

# Aplica as migrations pendentes no banco
dotnet ef database update \
  --project OrderEngine.Infrastructure/OrderEngine.Infrastructure.csproj \
  --startup-project OrderEngine.Api/OrderEngine.Api.csproj

# Desfaz a última migration ainda não aplicada (só se não foi aplicada no banco)
dotnet ef migrations remove \
  --project OrderEngine.Infrastructure/OrderEngine.Infrastructure.csproj \
  --startup-project OrderEngine.Api/OrderEngine.Api.csproj

# Volta o banco para uma migration anterior
dotnet ef database update <NomeDaMigrationAlvo> \
  --project OrderEngine.Infrastructure/OrderEngine.Infrastructure.csproj \
  --startup-project OrderEngine.Api/OrderEngine.Api.csproj

# Lista as migrations e o estado de cada uma
dotnet ef migrations list \
  --project OrderEngine.Infrastructure/OrderEngine.Infrastructure.csproj \
  --startup-project OrderEngine.Api/OrderEngine.Api.csproj

# Mantém a ferramenta global alinhada com o runtime (9.0.21)
dotnet tool update -g dotnet-ef --version 9.0.21
```

### Execução

```bash
# Restaura, compila a solução inteira
dotnet restore OrderEngine.sln
dotnet build OrderEngine.sln

# Roda a API (perfil Development, portas 5119 HTTP e 7110 HTTPS)
dotnet run --project OrderEngine.Api/OrderEngine.Api.csproj

# Roda o Worker (em outro terminal, para consumir a fila)
dotnet run --project OrderEngine.Worker/OrderEngine.Worker.csproj

# Exemplo de criação de pedido (idempotente)
curl -X POST http://localhost:5119/api/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: $(uuidgen)" \
  -d '{"items":[{"product":"Pão de queijo","quantity":2,"price":5.50}]}'

# Consulta um pedido (passa pelo cache-aside)
curl http://localhost:5119/api/orders/<guid>
```

### Testes

```bash
# Roda todos os testes (exige Docker com API 1.44 ou superior)
dotnet test OrderEngine.sln

# Só o projeto de integração, com saída detalhada
dotnet test OrderEngine.IntegrationTests/OrderEngine.IntegrationTests.csproj --verbosity normal

# Reexecuta sem recompilar, depois de um build já feito
dotnet test --no-build
```

### Git (fluxo de branches do projeto)

```bash
# Uma branch por fase, a partir da main
git checkout -b feature/<fase>-<nome> main

# Ao concluir a fase, merge na main
git checkout main && git merge --no-ff feature/<fase>-<nome>
```

---

## 7. Leituras Recomendadas para Aprofundamento

- **Transactional Outbox:** Chris Richardson, *Microservices Patterns*, capítulo sobre messaging.
- **Cache Stampede:** o artigo clássico de Vattani et al., *Optimal Probabilistic Cache Stampede Prevention* (VLDB 2015), que explica por que o jitter funciona.
- **Locks distribuídos e fencing tokens:** Martin Kleppmann, *How to do distributed locking*, e a discussão sobre o algoritmo Redlock.
- **Entregas at-least-once e idempotência:** Enterprise Integration Patterns (Hohpe e Woolf), capítulo sobre *Idempotent Receiver*.
- **Clean Architecture:** Robert C. Martin, *Clean Architecture*, e o artigo original de Uncle Bob sobre dependências apontando para dentro.
