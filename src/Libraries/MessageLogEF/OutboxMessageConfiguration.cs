using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ValetaxTest.MessageLogEF;

/// <summary>The <c>outbox_messages</c> table; applied by every service that publishes messages.</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).UseIdentityAlwaysColumn();
        builder.Property(message => message.Topic).HasMaxLength(200);
        builder.Property(message => message.Key).HasMaxLength(200);
        builder.Property(message => message.Type).HasMaxLength(200);
        builder.Property(message => message.Payload).HasColumnType("jsonb");
        builder.Property(message => message.TraceContext).HasColumnType("jsonb");
        builder.Property(message => message.CorrelationId).HasMaxLength(200);
        builder.Property(message => message.LastError).HasMaxLength(2000);
    }
}
