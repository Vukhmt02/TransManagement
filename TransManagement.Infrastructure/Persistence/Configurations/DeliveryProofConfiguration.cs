using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class DeliveryProofConfiguration : AuditableEntityConfiguration<DeliveryProof>
{
    public override void Configure(EntityTypeBuilder<DeliveryProof> builder)
    {
        base.Configure(builder);
        builder.ToTable("delivery_proofs");
        builder.HasIndex(x => x.RouteStopId).IsUnique();
        builder.Property(x => x.ReceiverName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PhotoUrl).HasMaxLength(2000);
        builder.Property(x => x.SignatureData).HasMaxLength(10000);
        builder.Property(x => x.Note).HasMaxLength(2000);
    }
}
