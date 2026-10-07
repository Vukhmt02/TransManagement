using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class RouteStopConfiguration : AuditableEntityConfiguration<RouteStop>
{
    public override void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        base.Configure(builder);

        builder.ToTable("route_stops");
        builder.HasIndex(stop => new { stop.ShipmentId, stop.Sequence }).IsUnique();

        builder.Property(stop => stop.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(stop => stop.Address).HasMaxLength(500).IsRequired();
        builder.Property(stop => stop.ContactName).HasMaxLength(200);
        builder.Property(stop => stop.ContactPhone).HasMaxLength(30);
        builder.Property(stop => stop.Latitude).HasPrecision(9, 6);
        builder.Property(stop => stop.Longitude).HasPrecision(9, 6);
        builder.Property(stop => stop.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(stop => stop.Shipment)
            .WithMany(shipment => shipment.RouteStops)
            .HasForeignKey(stop => stop.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stop => stop.TransportOrder)
            .WithMany(order => order.RouteStops)
            .HasForeignKey(stop => stop.TransportOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

