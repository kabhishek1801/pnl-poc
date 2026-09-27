using CsvHelper;
using CsvHelper.Configuration;
using PnL.Application.DTO;
using PnL.Application.Interfaces;
using System.Globalization;

namespace PnL.Infrastructure.Helpers;

public sealed class CsvFeedParser : ICsvFeedParser
{
    public async IAsyncEnumerable<PnLFeedRecord> ParseAsync(
        Stream csvStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var csv = new CsvReader(reader, configuration);
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceSystem = csv.GetField<string>("SourceSystem") ?? string.Empty;
            var accountNumber = csv.GetField<int>("AccountNumber");
            var pnlAmount = csv.GetField<int>("PnLAmount");

            yield return new PnLFeedRecord(sourceSystem, accountNumber, pnlAmount);
        }
    }
}
