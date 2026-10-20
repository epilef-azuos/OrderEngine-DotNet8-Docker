# Constituição: Regras Invioláveis de Domínio e Persistência

1. **Encapsulamento Estrito:** Entidades no `Domain` não podem ter propriedades com `set` ou `init` públicos. O estado só pode ser alterado através de métodos construtores ou comportamentais (ex: `AddOrderItem()`, `MarkAsCompleted()`).
2. **Validação no Domínio:** Uma entidade nunca deve nascer em um estado inválido. Construtores devem validar nulos, valores negativos ou regras fundamentais antes de instanciar.
3. **Isolamento de ORM:** O `Domain` não usa anotações de dados do EF Core (`[Table]`, `[Column]`, `[Key]`). Todo o mapeamento relacional deve ser feito via Fluent API (`IEntityTypeConfiguration<T>`) na camada de `Infrastructure`.
4. **Repositórios:** Os repositórios retornam e recebem apenas entidades de domínio. Eles ocultam completamente o `DbContext` do resto da aplicação.