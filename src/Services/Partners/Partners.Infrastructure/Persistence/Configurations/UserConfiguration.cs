using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValetaxTest.Core;
using ValetaxTest.Partners.Domain.Users;

namespace ValetaxTest.Partners.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Each row keeps only the direct partner; branches are read with recursive queries.
        // So changing a partner (which moves a whole subtree) updates a single row.
        builder.ToTable("users");
        builder.HasKey(user => user.ExternalId);
        builder.Property(user => user.ExternalId).HasMaxLength(ExternalIds.UserIdMaxLength);
        builder.Property(user => user.PartnerId).HasMaxLength(ExternalIds.UserIdMaxLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(user => user.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(user => user.PartnerId);
    }
}
