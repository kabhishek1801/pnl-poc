using Microsoft.EntityFrameworkCore;
using PnL.Application.Interfaces;
using PnL.Domain;
using PnL.Domain.Enums;

namespace PnL.Infrastructure.Persistence;

public sealed class PnLRepository(PnLDbContext dbContext) : IPnLRepository
{
    public async Task<PnLRecord> UpsertAsync(
        PnLRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var existing = await dbContext.PnLRecords
            .SingleOrDefaultAsync(
                storedRecord => storedRecord.AccountNumber == record.AccountNumber,
                cancellationToken);

        if (existing is null)
        {
            dbContext.PnLRecords.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
            return record;
        }

        existing.Update(
            record.SourceSystem,
            record.PnLAmount,
            record.FeedSource,
            record.BatchId,
            record.CapturedAtUtc);

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<(IReadOnlyList<PnLRecord> Records, int TotalCount)> GetPageAsync(
        PnLStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var query = dbContext.PnLRecords
            .Where(record => record.Status == status)
            .OrderByDescending(record => record.CapturedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (records, totalCount);
    }
}
