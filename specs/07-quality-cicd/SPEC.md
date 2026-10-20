# Spec: Fase 07 - Qualidade (Integration Tests) e CI/CD

## 1. Visão Geral
Garantir que a arquitetura distribuída (API, Banco de Dados, Cache e Mensageria) funciona como um ecossistema coeso. Em vez de dependermos apenas de testes unitários isolados, implementaremos Testes de Integração com infraestrutura real usando Testcontainers, orquestrados por uma pipeline de Continuous Integration (CI) no GitHub Actions.

## 2. Requisitos Não-Funcionais e Negócio
- **Testes Ephemeral:** Os testes não podem depender da infraestrutura local do programador. Devem subir instâncias limpas do PostgreSQL, Redis e RabbitMQ via Docker durante a execução.
- **Integração End-to-End (E2E) Sintética:** O teste deve chamar o endpoint `POST /api/orders` em memória (usando `WebApplicationFactory`), verificar a resposta HTTP e validar se o pedido foi salvo no banco de dados.
- **Continuous Integration:** Qualquer *Push* ou *Pull Request* para a `main` deve acionar um *workflow* no GitHub Actions que faz Build e corre os Testes.

## 3. Critérios de Aceite
- [ ] O projeto `OrderEngine.IntegrationTests` está criado e compila.
- [ ] Os testes executam com sucesso utilizando Testcontainers.
- [ ] O ficheiro `.github/workflows/ci.yml` está configurado.
- [ ] O pipeline do GitHub Actions roda com sucesso na nuvem.