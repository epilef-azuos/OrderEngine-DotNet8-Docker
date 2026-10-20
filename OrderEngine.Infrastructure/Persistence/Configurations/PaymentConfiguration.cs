using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderEngine.Domain.Entities;

namespace OrderEngine.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
