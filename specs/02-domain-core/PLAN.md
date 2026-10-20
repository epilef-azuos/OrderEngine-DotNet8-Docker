# Plan: Detalhamento Técnico e Decisões - Fase 02

## 1. Pacotes NuGet Necessários
No projeto `OrderEngine.Infrastructure`:
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Npgsql.EntityFrameworkCore.PostgreSQL`

No projeto `OrderEngine.Api` (necessário para rodar as migrations na inicialização):
- `Microsoft.EntityFrameworkCore.Design`

## 2. Estrutura do Domain (`OrderEngine.Domain`)
- `Entities/Order.cs`
- `Entities/OrderItem.cs`
- `Entities/Payment.cs`
- `Enums/OrderStatus.cs`
- `Enums/PaymentStatus.cs`
- `Interfaces/Repositories/IOrderRepository.cs`

## 3. Estrutura da Infrastructure (`OrderEngine.Infrastructure`)
- `Persistence/OrderDbContext.cs`
- `Persistence/Configurations/OrderConfiguration.cs`
- `Persistence/Configurations/PaymentConfiguration.cs`
- `Persistence/Repositories/OrderRepository.cs`