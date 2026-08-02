using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Reporting.Services
{
    public interface ICsvService
    {
        Task<byte[]> GenerateCsvAsync<T>(IEnumerable<T> data);
        Task<byte[]> GenerateCsvWithHeadersAsync<T>(IEnumerable<T> data, Dictionary<string, string> columnHeaders);
    }

    public class CsvService : ICsvService
    {
        private readonly ILogger<CsvService> _logger;

        public CsvService(ILogger<CsvService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateCsvAsync<T>(IEnumerable<T> data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new StreamWriter(memoryStream))
                    using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        Delimiter = ",",
                        HasHeaderRecord = true,
                        TrimOptions = TrimOptions.Trim
                    }))
                    {
                        csv.WriteRecords(data);
                        writer.Flush();
                        var bytes = memoryStream.ToArray();

                        _logger.LogInformation("CSV file generated with {Count} records", data.Count());
                        return bytes;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate CSV file");
                    throw;
                }
            });
        }

        public async Task<byte[]> GenerateCsvWithHeadersAsync<T>(IEnumerable<T> data, Dictionary<string, string> columnHeaders)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new StreamWriter(memoryStream))
                    using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        Delimiter = ",",
                        HasHeaderRecord = true,
                        TrimOptions = TrimOptions.Trim
                    }))
                    {
                        // Write custom headers
                        foreach (var header in columnHeaders)
                        {
                            csv.WriteField(header.Value);
                        }
                        csv.NextRecord();

                        // Write data
                        csv.WriteRecords(data);
                        writer.Flush();
                        var bytes = memoryStream.ToArray();

                        _logger.LogInformation("CSV file with custom headers generated with {Count} records", data.Count());
                        return bytes;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate CSV file with custom headers");
                    throw;
                }
            });
        }
    }
}
