# Spec: Fase 06 - Worker Assíncrono e Controlo de Concorrência

## 1. Visão Geral
Implementar o consumidor que irá processar as mensagens de pedidos em segundo plano. Este serviço deve garantir que a baixa de stock ocorra de forma segura, evitando *Race Conditions* em ambientes de alta concorrência. 

## 2. Requisitos Não-Funcionais e Negócio
- **Padrão Competing Consumers:** O Worker vai ligar-se à *queue* vinculada à *exchange* `order.events`.
- **Distributed Lock para Stock:** Antes de processar os itens de um pedido, o Worker deve adquirir um Lock exclusivo no Redis com o nome do produto (ex: `lock:stock:Pão de queijo`). Se outro pedido tentar comprar o mesmo produto, deve aguardar a libertação do Lock.
- **Idempotência no Consumo:** O Worker deve guardar num cache (ou base de dados) o ID do pedido que já processou para evitar reprocessar a mesma mensagem caso o RabbitMQ envie em duplicado (*At-Least-Once delivery*).
- **Simulação de Processamento:** O Worker irá alterar o status do `Order` no PostgreSQL de `Pending` para `Processing` e, após o "pagamento", para `Completed`.

## 3. Critérios de Aceite
- [ ] O projeto `OrderEngine.Worker` arranca sem erros e liga-se ao RabbitMQ.
- [ ] Quando um pedido é criado na API, o Worker consome a mensagem e regista no *log* "Processing Order [ID]".
- [ ] O banco de dados reflete a alteração de status do pedido para `Completed`.