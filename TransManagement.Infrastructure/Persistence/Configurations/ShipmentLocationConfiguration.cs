using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class ShipmentLocationConfiguration : AuditableEntityConfiguration<ShipmentLocation>
{
    public override void Configure(EntityTypeBuilder<ShipmentLocation> builder)
    {
        base.Configure(builder);
        builder.ToTable("shipment_locations");
        builder.HasIndex(x => new { x.ShipmentId, x.RecordedAtUtc });
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.SpeedKph).HasPrecision(8, 2);
        builder.Property(x => x.AccuracyMeters).HasPrecision(8, 2);
        builder.HasOne(x => x.Shipment).WithMany(x => x.Locations)
            .HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Driver).WithMany()
            .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
    }
}
