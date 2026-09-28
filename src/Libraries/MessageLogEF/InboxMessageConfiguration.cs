using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ValetaxTest.MessageLogEF;

/// <summary>The <c>inbox_messages</c> table; applied by every service that consumes messages.</summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(message => new { message.MessageId, message.Consumer });
        builder.Property(message => message.Consumer).HasMaxLength(200);
        builder.Property(message => message.MessageType).HasMaxLength(200);
        builder.HasIndex(message => message.ProcessedAt);
    }
}
