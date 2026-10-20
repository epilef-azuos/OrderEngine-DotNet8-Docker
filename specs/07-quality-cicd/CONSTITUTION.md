# Constituição: Regras de Testes e CI

1. **Zero Mocks para Infraestrutura (em Testes de Integração):** É expressamente proibido fazer `Mock<IOrderDbContext>` ou `Mock<IConnectionMultiplexer>` nos testes de integração. A regra de ouro é: testa contra a realidade.
2. **Isolamento de Estado:** Os testes devem poder rodar em paralelo sem que os dados de um afetem o outro. No nosso caso, um banco de dados novo gerado pelo Testcontainers por ciclo de teste garante isso.
3. **Pipeline Quebrada é Bloqueio:** O código não pode ser integrado (`merge`) na `main` se a pipeline de CI falhar. O `.github/workflows/ci.yml` é a autoridade máxima de qualidade.