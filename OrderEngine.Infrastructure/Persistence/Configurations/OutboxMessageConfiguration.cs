using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderEngine.Domain.Entities;

namespace OrderEngine.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.OccurredOn)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.ProcessedOn)
            .HasColumnType("timestamp with time zone");

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        // Índice parcial: o worker só consulta mensagens ainda não processadas.
        builder.HasIndex(m => m.OccurredOn)
            .HasFilter("\"ProcessedOn\" IS NULL");
    }
}
