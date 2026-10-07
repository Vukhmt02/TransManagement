using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : AuditableEntityConfiguration<Vehicle>
{
    public override void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        base.Configure(builder);

        builder.ToTable("vehicles");
        builder.HasIndex(vehicle => vehicle.LicensePlate).IsUnique();

        builder.Property(vehicle => vehicle.LicensePlate).HasMaxLength(20).IsRequired();
        builder.Property(vehicle => vehicle.VehicleType).HasMaxLength(100).IsRequired();
        builder.Property(vehicle => vehicle.MaxLoadKg).HasPrecision(12, 2);
        builder.Property(vehicle => vehicle.CargoVolumeM3).HasPrecision(10, 2);
        builder.Property(vehicle => vehicle.Status).HasConversion<string>().HasMaxLength(30);
    }
}

