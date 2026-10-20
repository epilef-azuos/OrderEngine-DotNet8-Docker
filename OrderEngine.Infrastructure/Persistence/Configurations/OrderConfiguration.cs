using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderEngine.Domain.Entities;

namespace OrderEngine.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(o => o.TotalAmount);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("OrderItems");

            item.WithOwner().HasForeignKey("OrderId");

            item.HasKey(i => i.Id);

            item.Property(i => i.Id).ValueGeneratedNever();

            item.Property(i => i.Product)
                .HasMaxLength(200)
                .IsRequired();

            item.Property(i => i.Quantity).IsRequired();

            item.Property(i => i.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            item.Ignore(i => i.Subtotal);
        });
    }
}
