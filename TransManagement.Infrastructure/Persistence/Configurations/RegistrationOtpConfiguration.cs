using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Infrastructure.Identity;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class RegistrationOtpConfiguration : IEntityTypeConfiguration<RegistrationOtp>
{
    public void Configure(EntityTypeBuilder<RegistrationOtp> builder)
    {
        builder.ToTable("registration_otps");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Salt).HasMaxLength(64).IsRequired();
    }
}
