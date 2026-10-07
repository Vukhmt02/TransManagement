using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class ShipmentOrderConfiguration : AuditableEntityConfiguration<ShipmentOrder>
{
    public override void Configure(EntityTypeBuilder<ShipmentOrder> builder)
    {
        base.Configure(builder);

        builder.ToTable("shipment_orders");
        builder.HasIndex(item => new { item.ShipmentId, item.TransportOrderId }).IsUnique();
        builder.HasIndex(item => new { item.ShipmentId, item.Sequence }).IsUnique();

        builder.HasOne(item => item.Shipment)
            .WithMany(shipment => shipment.ShipmentOrders)
            .HasForeignKey(item => item.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.TransportOrder)
            .WithMany(order => order.ShipmentOrders)
            .HasForeignKey(item => item.TransportOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

