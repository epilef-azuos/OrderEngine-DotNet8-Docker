# Spec: Fase 02 - Core do Domínio e Persistência Relacional

## 1. Visão Geral
Implementar o coração da aplicação usando conceitos de Domain-Driven Design (DDD) e preparar a infraestrutura de persistência relacional com o Entity Framework Core (PostgreSQL). O objetivo é garantir que as regras de negócio de pedidos e pagamentos estejam encapsuladas e isoladas de detalhes de banco de dados.

## 2. Requisitos de Negócio (Entidades)
- **Order (Pedido):**
  - Deve possuir um Identificador Único (Guid).
  - Deve possuir um Status (`Pending`, `Processing`, `Completed`, `Failed`).
  - Deve conter uma lista de itens (Product, Quantity, Price).
  - Deve calcular o valor total (TotalAmount) internamente.
- **Payment (Pagamento):**
  - Relacionado a um OrderId.
  - Deve possuir um Status (`Pending`, `Approved`, `Rejected`).
  - Deve possuir um IdempotencyKey para evitar duplo processamento.

## 3. Critérios de Aceite
- [ ] As entidades `Order` e `Payment` estão no projeto `Domain` com propriedades encapsuladas (sem `set` público).
- [ ] O projeto `Infrastructure` contém o `OrderDbContext` configurado para PostgreSQL.
- [ ] O mapeamento das entidades (Fluent API) está configurado no `Infrastructure`.
- [ ] O banco de dados PostgreSQL recebe as tabelas via rodada de `dotnet ef database update`.