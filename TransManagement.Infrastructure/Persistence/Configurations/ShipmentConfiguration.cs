using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : AuditableEntityConfiguration<Shipment>
{
    public override void Configure(EntityTypeBuilder<Shipment> builder)
    {
        base.Configure(builder);

        builder.ToTable("shipments");
        builder.HasIndex(shipment => shipment.Code).IsUnique();
        builder.HasIndex(shipment => new { shipment.VehicleId, shipment.PlannedDepartureAtUtc });
        builder.HasIndex(shipment => new { shipment.DriverId, shipment.PlannedDepartureAtUtc });

        builder.Property(shipment => shipment.Code).HasMaxLength(40).IsRequired();
        builder.Property(shipment => shipment.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(shipment => shipment.Note).HasMaxLength(2000);

        builder.HasOne(shipment => shipment.Vehicle)
            .WithMany(vehicle => vehicle.Shipments)
            .HasForeignKey(shipment => shipment.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(shipment => shipment.Driver)
            .WithMany(driver => driver.Shipments)
            .HasForeignKey(shipment => shipment.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

