# Constituição: Regras de Leitura e Cache

1. **Separação Command/Query:** *Queries* (leituras) não devem partilhar os mesmos modelos de retorno das Entidades de Domínio. Devem retornar DTOs (ex: `OrderResponseDto`).
2. **Read-Only Context:** Ao usar o EF Core para leituras (Queries), é obrigatório o uso de `.AsNoTracking()` para evitar overhead no *Change Tracker*.
3. **Resiliência Primária:** Se o servidor Redis estiver indisponível, a aplicação não deve falhar (*Crash*). O sistema deve efetuar *fallback* gracioso para a base de dados com um aviso (*Warning log*), garantindo a disponibilidade.