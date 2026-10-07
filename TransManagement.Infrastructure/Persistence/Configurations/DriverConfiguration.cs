using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class DriverConfiguration : AuditableEntityConfiguration<Driver>
{
    public override void Configure(EntityTypeBuilder<Driver> builder)
    {
        base.Configure(builder);

        builder.ToTable("drivers");
        builder.HasIndex(driver => driver.EmployeeCode).IsUnique();
        builder.HasIndex(driver => driver.LicenseNumber).IsUnique();

        builder.Property(driver => driver.EmployeeCode).HasMaxLength(32).IsRequired();
        builder.Property(driver => driver.FullName).HasMaxLength(200).IsRequired();
        builder.Property(driver => driver.Phone).HasMaxLength(30).IsRequired();
        builder.Property(driver => driver.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.Property(driver => driver.LicenseClass).HasMaxLength(20).IsRequired();
        builder.Property(driver => driver.Status).HasConversion<string>().HasMaxLength(30);
    }
}

