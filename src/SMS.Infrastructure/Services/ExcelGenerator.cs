using OfficeOpenXml;
using SMS.Domain.Interfaces;

namespace SMS.Infrastructure.Services
{
    public class ExcelGenerator : IExcelGenerator
    {
        private readonly ILogger<ExcelGenerator> _logger;

        public ExcelGenerator(ILogger<ExcelGenerator> logger)
        {
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> GenerateExcelAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1")
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                var properties = typeof(T).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = properties[i].Name;
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                var rowIndex = 2;
                foreach (var item in data)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var value = properties[i].GetValue(item);
                        worksheet.Cells[rowIndex, i + 1].Value = value?.ToString();
                    }
                    rowIndex++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Excel file");
                throw;
            }
        }

        public async Task<byte[]> GenerateExcelWithHeadersAsync<T>(IEnumerable<T> data, Dictionary<string, string> headers, string sheetName = "Sheet1")
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                var properties = typeof(T).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    var headerName = headers.TryGetValue(properties[i].Name, out var value) ? value : properties[i].Name;
                    worksheet.Cells[1, i + 1].Value = headerName;
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                var rowIndex = 2;
                foreach (var item in data)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var value = properties[i].GetValue(item);
                        worksheet.Cells[rowIndex, i + 1].Value = value?.ToString();
                    }
                    rowIndex++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Excel file with headers");
                throw;
            }
        }

        public async Task<byte[]> GenerateReportAsync<T>(IEnumerable<T> data, string title, Dictionary<string, string> headers)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Report");

                // Title
                worksheet.Cells[1, 1].Value = title;
                worksheet.Cells[1, 1, 1, 10].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
                worksheet.Cells[2, 1, 2, 10].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                var properties = typeof(T).GetProperties();
                var startRow = 4;

                for (int i = 0; i < properties.Length; i++)
                {
                    var headerName = headers.TryGetValue(properties[i].Name, out var value) ? value : properties[i].Name;
                    worksheet.Cells[startRow, i + 1].Value = headerName;
                    worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                }

                var rowIndex = startRow + 1;
                foreach (var item in data)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var value = properties[i].GetValue(item);
                        worksheet.Cells[rowIndex, i + 1].Value = value?.ToString();
                    }
                    rowIndex++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Excel report");
                throw;
            }
        }
    }
}