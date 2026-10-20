# Spec: Fase 04 - Cache Resiliente e Proteção de Base de Dados

## 1. Visão Geral
Implementar um endpoint de leitura de pedidos (`GET /api/orders/{id}`) altamente otimizado. Para proteger a base de dados relacional de avalanches de consultas simultâneas (Cache Stampede / Thundering Herd), este endpoint deve usar o Redis com padrões avançados de resiliência.

## 2. Requisitos Não-Funcionais e Negócio
- **CQRS:** O endpoint de leitura usa uma *Query* via MediatR (`GetOrderByIdQuery`).
- **Cache Distribuído:** Os detalhes do pedido devem ser cacheados no Redis.
- **TTL com Jitter:** O tempo de vida do cache (TTL) deve ter uma pequena variação aleatória (Jitter) adicionada ao tempo base para evitar que múltiplas chaves expirem exata e simultaneamente.
- **Distributed Lock (Mutex) para Recarga:** Se o cache expirar e milhares de requisições pedirem o mesmo ID simultaneamente, apenas UMA requisição deve ir à base de dados (PostgreSQL) recarregar a informação. As outras devem aguardar brevemente e ler do cache recém-preenchido.

## 3. Critérios de Aceite
- [ ] O endpoint `GET /api/orders/{id}` está disponível e retorna os dados corretos.
- [ ] Múltiplas chamadas sucessivas ao mesmo ID não disparam *logs* do Entity Framework efetuando `SELECT` na base de dados (apenas a primeira chamada deve ir à base de dados).
- [ ] A lógica de recarga utiliza um mecanismo de trava (Lock) seguro com o Redis.