# Checklist de Execução - Fase 07

- [x] **T1:** Criar a branch `feature/07-quality-cicd` a partir da `main`.
- [x] **T2:** Criar o projeto xUnit `OrderEngine.IntegrationTests` e adicioná-lo à solução. Adicionar referências para `OrderEngine.Api` e `OrderEngine.Infrastructure`.
- [x] **T3:** Instalar os pacotes `Testcontainers.*`, `FluentAssertions` e `Microsoft.AspNetCore.Mvc.Testing` no projeto de testes.
- [x] **T4:** Expor os *internals* da API (no `Program.cs` adicionar `public partial class Program { }`) para o `WebApplicationFactory` ter acesso.
- [x] **T5:** Implementar o `IntegrationTestWebAppFactory` inicializando os três containers e substituindo a configuração.
- [x] **T6:** Escrever o `CreateOrderTests`. Enviar um POST com um `Idempotency-Key` e confirmar que retorna 202. Fazer uma consulta direta ao BD de teste injetado para garantir que a tabela `Orders` tem o registo.
- [x] **T7:** Criar o diretório `.github/workflows` na raiz do projeto e adicionar o ficheiro `ci.yml`.
- [x] **T8:** Executar localmente (`dotnet test`), fazer *commit*, dar *push* da branch para o GitHub, abrir o PR (ou ver a Action a rodar via Push) e garantir que a tab "Actions" fica verde.