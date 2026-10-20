# Constituição: Regras Invioláveis de Engenharia

1. **Agnosticismo do Domínio**: O projeto `OrderEngine.Domain` não pode referenciar *nenhum* pacote do Entity Framework Core, Dapper, Redis, RabbitMQ ou qualquer detalhe de infraestrutura.
2. **Injeção de Dependência Frontal**: As referências para o projeto `OrderEngine.Infrastructure` só devem existir nos projetos de inicialização (`Api` e `Worker`). A camada `Application` jamais deve conhecer implementações concretas da infraestrutura.
3. **Infraestrutura Efêmera**: Nenhuma configuração local (connection strings, senhas) deve ser hardcoded no código fonte. Tudo deve ser injetado via variáveis de ambiente ou `appsettings.Development.json`.
4. **Disciplina de Branch**: Todo trabalho inicia em uma branch `feature/XX-nome-da-tarefa`. O código só vai para a `main` com build passando.