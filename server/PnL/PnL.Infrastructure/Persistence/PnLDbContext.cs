using Microsoft.EntityFrameworkCore;
using PnL.Domain;

namespace PnL.Infrastructure.Persistence;

public sealed class PnLDbContext(DbContextOptions<PnLDbContext> options) : DbContext(options)
{
    public DbSet<PnLRecord> PnLRecords => Set<PnLRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all entity configurations from the current assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PnLDbContext).Assembly);
    }
}
