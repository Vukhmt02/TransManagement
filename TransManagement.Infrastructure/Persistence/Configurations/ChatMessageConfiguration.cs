using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransManagement.Domain.Entities;
using TransManagement.Infrastructure.Identity;

namespace TransManagement.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : AuditableEntityConfiguration<ChatMessage>
{
    public override void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable("chat_messages");

        builder.HasIndex(message => new { message.ConversationId, message.SentAtUtc });
        builder.HasIndex(message => new { message.ConversationId, message.ReadAtUtc });

        builder.Property(message => message.Content)
            .HasMaxLength(2000)
            .IsRequired();
        builder.Property(message => message.MessageType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(message => message.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
