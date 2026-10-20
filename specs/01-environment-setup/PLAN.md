# Plan: Detalhamento Técnico e Decisões - Fase 01

## 1. Estrutura de Diretórios da Clean Architecture
A solução será dividida em projetos de classe (.NET 8):
- `OrderEngine.Domain`: Entidades, Value Objects, Interfaces de Repositório (Sem dependências externas).
- `OrderEngine.Application`: Casos de uso, Commands, Queries, Behaviors do MediatR. Depende do `Domain`.
- `OrderEngine.Infrastructure`: Implementações de persistência (EF Core), mensageria (RabbitMQ) e cache (Redis). Depende do `Application`.
- `OrderEngine.Api`: Web API, Controllers/Endpoints, Injeção de Dependência. Depende do `Application` e `Infrastructure`.
- `OrderEngine.Worker`: Background Service para processamento de filas. Depende do `Application` e `Infrastructure`.

## 2. Configurações de Infraestrutura (Docker Compose)
- **PostgreSQL**: Imagem `postgres:16-alpine`. Portas: `5432:5432`.
- **Redis**: Imagem `redis:7-alpine`. Portas: `6379:6379`.
- **RabbitMQ**: Imagem `rabbitmq:3-management-alpine`. Portas: `5672:5672` (AMQP) e `15672:15672` (UI).

## 3. Comandos CLI Base
```bash
dotnet new sln -n OrderEngine
dotnet new classlib -n OrderEngine.Domain
dotnet new classlib -n OrderEngine.Application
dotnet new classlib -n OrderEngine.Infrastructure
dotnet new webapi -n OrderEngine.Api
dotnet new worker -n OrderEngine.Worker
# Vinculação dos projetos à solution e referências entre eles serão feitas na T1.