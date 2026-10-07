using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class TransportOrderConfiguration : AuditableEntityConfiguration<TransportOrder>
{
    public override void Configure(EntityTypeBuilder<TransportOrder> builder)
    {
        base.Configure(builder);

        builder.ToTable("transport_orders");
        builder.HasIndex(order => order.Code).IsUnique();
        builder.HasIndex(order => new { order.CustomerId, order.Status });

        builder.Property(order => order.Code).HasMaxLength(40).IsRequired();
        builder.Property(order => order.PickupAddress).HasMaxLength(500).IsRequired();
        builder.Property(order => order.DeliveryAddress).HasMaxLength(500).IsRequired();
        builder.Property(order => order.SenderName).HasMaxLength(200);
        builder.Property(order => order.SenderPhone).HasMaxLength(30);
        builder.Property(order => order.RecipientName).HasMaxLength(200);
        builder.Property(order => order.RecipientPhone).HasMaxLength(30);
        builder.Property(order => order.GoodsDescription).HasMaxLength(1000).IsRequired();
        builder.Property(order => order.WeightKg).HasPrecision(12, 2);
        builder.Property(order => order.VolumeM3).HasPrecision(10, 2);
        builder.Property(order => order.EstimatedPrice).HasPrecision(18, 2);
        builder.Property(order => order.Note).HasMaxLength(2000);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(40);

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.TransportOrders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

