using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PnL.Domain;

namespace PnL.Infrastructure.Persistence.Configurations;

public sealed class PnLRecordConfiguration : IEntityTypeConfiguration<PnLRecord>
{
    public void Configure(EntityTypeBuilder<PnLRecord> builder)
    {
        builder.ToTable("PnLRecords");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.AccountNumber)
            .IsRequired();

        builder.HasIndex(record => record.AccountNumber)
            .IsUnique();

        builder.Property(record => record.SourceSystem)
            .IsRequired();
    }
}
