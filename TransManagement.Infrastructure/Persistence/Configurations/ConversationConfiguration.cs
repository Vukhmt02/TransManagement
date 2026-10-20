using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;
using TransManagement.Infrastructure.Identity;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : AuditableEntityConfiguration<Conversation>
{
    public override void Configure(EntityTypeBuilder<Conversation> builder)
    {
        base.Configure(builder);

        builder.ToTable("conversations");

        builder.HasIndex(conversation => new { conversation.CustomerUserId, conversation.Status });
        builder.HasIndex(conversation => new { conversation.AssignedAdminUserId, conversation.Status });

        builder.Property(conversation => conversation.Subject)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(conversation => conversation.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(conversation => conversation.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(conversation => conversation.AssignedAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne(message => message.Conversation)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
