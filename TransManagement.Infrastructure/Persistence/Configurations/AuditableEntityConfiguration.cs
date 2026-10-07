using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Common;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public abstract class AuditableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : AuditableEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
        builder.Property(entity => entity.CreatedBy).HasMaxLength(100);
        builder.Property(entity => entity.UpdatedBy).HasMaxLength(100);
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false);

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

