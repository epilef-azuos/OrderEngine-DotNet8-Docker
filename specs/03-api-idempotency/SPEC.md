# Spec: Fase 03 - API de Recepção e Idempotência

## 1. Visão Geral
Construir o endpoint REST para recepção de novos pedidos de forma resiliente. A API deve atuar apenas como uma fachada (Facade), delegando a regra de orquestração para a camada de Application via CQRS. Além disso, todas as requisições de criação devem ser idempotentes, garantindo que o envio repetido da mesma carga útil (com a mesma chave) não duplique a operação.

## 2. Requisitos Não-Funcionais e Negócio
- **CQRS:** O endpoint deve despachar um `CreateOrderCommand` via MediatR e não deve conter lógica de negócio.
- **Idempotência:** O cabeçalho HTTP `Idempotency-Key` é obrigatório no endpoint `POST /api/orders`.
- **Redis:** A verificação de idempotência deve consultar o Redis. Se a chave existir, o endpoint deve retornar a resposta armazenada anteriormente em cache (ex: HTTP 200 OK com o ID do pedido gerado) em vez de reprocessar. Se não existir, processa e salva no Redis por 24 horas.
- **Resposta Rápida:** Como o processamento pesado (pagamento, estoque) será assíncrono (futuro), a API salva no banco o pedido como `Pending` e responde rapidamente.

## 3. Critérios de Aceite
- [ ] O Swagger UI (ou endpoint) está acessível e expõe `POST /api/orders`.
- [ ] Requisições sem o header `Idempotency-Key` retornam HTTP 400 Bad Request.
- [ ] Duas requisições sequenciais com o mesmo body e mesma `Idempotency-Key` geram apenas 1 registro no PostgreSQL. A segunda retorna o mesmo ID gerado pela primeira.