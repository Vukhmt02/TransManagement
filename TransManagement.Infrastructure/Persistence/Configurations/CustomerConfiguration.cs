using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : AuditableEntityConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        base.Configure(builder);

        builder.ToTable("customers");
        builder.HasIndex(customer => customer.Code).IsUnique();

        builder.Property(customer => customer.Code).HasMaxLength(32).IsRequired();
        builder.Property(customer => customer.Name).HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.Phone).HasMaxLength(30).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(254);
        builder.Property(customer => customer.TaxCode).HasMaxLength(32);
        builder.Property(customer => customer.Address).HasMaxLength(500);
        builder.Property(customer => customer.IsActive).HasDefaultValue(true);
    }
}

