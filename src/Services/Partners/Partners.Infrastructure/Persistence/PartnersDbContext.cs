using Microsoft.EntityFrameworkCore;
using ValetaxTest.MessageLogEF;
using ValetaxTest.Partners.Domain.Users;

namespace ValetaxTest.Partners.Infrastructure.Persistence;

public sealed class PartnersDbContext(DbContextOptions<PartnersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartnersDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
