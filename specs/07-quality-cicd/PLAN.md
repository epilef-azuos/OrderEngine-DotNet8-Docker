# Plan: Detalhamento Técnico - Fase 07

## 1. Pacotes NuGet Necessários (`OrderEngine.IntegrationTests`)
- `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`
- `FluentAssertions` (Para validações fluentes e legíveis).
- `Microsoft.AspNetCore.Mvc.Testing` (Para o TestServer em memória).
- `Testcontainers`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`, `Testcontainers.RabbitMq`.

## 2. Estrutura do Projeto de Testes
- `IntegrationTests/Setup/IntegrationTestWebAppFactory.cs`: Herda de `WebApplicationFactory<Program>`, inicializa os 3 Testcontainers no `InitializeAsync`, e substitui as *connection strings* do `appsettings.json` em memória para apontar para os contentores efémeros.
- `IntegrationTests/Setup/BaseIntegrationTest.cs`: Classe base que injeta o HttpClient e o Factory.
- `IntegrationTests/Orders/CreateOrderTests.cs`: Classe com o teste focado na Fase 03 e Fase 05.

## 3. Estrutura do GitHub Actions
- `.github/workflows/ci.yml`:
  - Triggers: `push` e `pull_request` na branch `main`.
  - Steps: `actions/checkout`, `actions/setup-dotnet`, `dotnet build`, `dotnet test`.