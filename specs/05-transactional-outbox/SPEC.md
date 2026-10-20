# Spec: Fase 05 - Transactional Outbox Pattern

## 1. Visão Geral
Garantir que nenhum evento de criação de pedido se perca, mesmo diante de falhas severas de rede com o Message Broker (RabbitMQ). Implementaremos o padrão Transactional Outbox para persistir os eventos de domínio na mesma transação ACID do pedido relacional.

## 2. Requisitos Não-Funcionais e Negócio
- **Atomicidade:** O Pedido (`Orders`) e o Evento (`OutboxMessages`) devem ser salvos no PostgreSQL na mesma transação (`SaveChanges`).
- **Resiliência:** Se o RabbitMQ cair, o sistema continua a aceitar pedidos. Os eventos ficarão acumulados no Outbox e serão despachados quando o Broker voltar.
- **Background Processing:** Um serviço em segundo plano (`IHostedService`) deve varrer a tabela de Outbox periodicamente, publicar no RabbitMQ e marcar a mensagem como `ProcessedOn`.

## 3. Critérios de Aceite
- [ ] Tabela `OutboxMessages` criada na base de dados.
- [ ] A criação de um pedido via API gera um registo em `Orders` e um em `OutboxMessages`.
- [ ] O `OutboxBackgroundService` lê a mensagem e publica numa *Exchange* (ex: `order.events`) do RabbitMQ.