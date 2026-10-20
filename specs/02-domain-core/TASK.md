# Checklist de Execução - Fase 02

- [ ] **T1:** Criar a branch `feature/02-domain-core` a partir da `main`.
- [ ] **T2:** Instalar os pacotes NuGet do EF Core e Npgsql no projeto `OrderEngine.Infrastructure` e o pacote Design no projeto `OrderEngine.Api`.
- [ ] **T3:** Criar as entidades `Order`, `OrderItem`, `Payment` e seus Enums no projeto `OrderEngine.Domain`, respeitando a regra de encapsulamento (sem sets públicos).
- [ ] **T4:** Criar as interfaces de repositório (`IOrderRepository`, `IPaymentRepository`) no projeto `OrderEngine.Domain`.
- [ ] **T5:** Criar o `OrderDbContext` e as classes de mapeamento (`IEntityTypeConfiguration`) para o EF Core no projeto `OrderEngine.Infrastructure`.
- [ ] **T6:** Configurar a Injeção de Dependência do EF Core no `Program.cs` da API conectando ao PostgreSQL usando a connection string do banco local.
- [ ] **T7:** Gerar a primeira migration (`dotnet ef migrations add InitialCreate`) e aplicar no banco de dados (`dotnet ef database update`).
- [ ] **T8:** Executar o build, commitar as mudanças e fazer o merge para a branch `main`.