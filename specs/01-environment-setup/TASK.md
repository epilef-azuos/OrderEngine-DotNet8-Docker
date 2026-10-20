# Checklist de Execução - Fase 01

- [x] **T1:** Criar um repositório Git vazio, adicionar o `.gitignore` padrão para C# e criar a branch `feature/01-infra-setup`.
- [x] **T2:** Criar o arquivo `docker-compose.yml` na raiz do projeto contendo as definições do PostgreSQL, Redis e RabbitMQ (incluindo volumes locais e variáveis de ambiente).
- [x] **T3:** Executar `docker-compose up -d` e validar acesso ao Management UI do RabbitMQ no navegador (http://localhost:15672).
- [x] **T4:** Usar a CLI do .NET para criar a solução (`OrderEngine.sln`) e os 5 projetos especificados no `PLAN.md`.
- [x] **T5:** Adicionar as referências corretas entre os projetos da solução para refletir a Clean Architecture.
- [x] **T6:** Executar `dotnet build`, confirmar sucesso, comitar (ex: `feat: add docker infrastructure and solution boilerplate`) e realizar o merge na branch `main`.