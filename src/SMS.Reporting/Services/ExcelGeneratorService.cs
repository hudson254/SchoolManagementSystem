using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SMS.Reporting.Services
{
    public class ExcelGeneratorService : IExcelGenerator
    {
        private readonly ILogger<ExcelGeneratorService> _logger;

        public ExcelGeneratorService(ILogger<ExcelGeneratorService> logger)
        {
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> GenerateExcelFromDataAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1")
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add(sheetName);
                        var list = data.ToList();

                        if (list.Count == 0)
                        {
                            _logger.LogWarning("No data to export to Excel");
                            return package.GetAsByteArray();
                        }

                        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead)
                            .ToList();

                        for (int i = 0; i < properties.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = properties[i].Name;
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        for (int row = 0; row < list.Count; row++)
                        {
                            for (int col = 0; col < properties.Count; col++)
                            {
                                var value = properties[col].GetValue(list[row]);
                                worksheet.Cells[row + 2, col + 1].Value = value?.ToString() ?? string.Empty;
                            }
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        _logger.LogInformation("Excel file generated with {RowCount} rows", list.Count);
                        return package.GetAsByteArray();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate Excel file");
                    throw;
                }
            });
        }

        public async Task<byte[]> GenerateStudentReportExcelAsync(object reportData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("StudentReport");

                        worksheet.Cells[1, 1].Value = "Student Report";
                        worksheet.Cells[1, 1].Style.Font.Bold = true;
                        worksheet.Cells[1, 1].Style.Font.Size = 14;

                        _logger.LogInformation("Student report Excel file generated");
                        return package.GetAsByteArray();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate student report Excel file");
                    throw;
                }
            });
        }

        public async Task<byte[]> GenerateTableExcelAsync(
            string sheetName,
            IReadOnlyList<string> columns,
            IReadOnlyList<IReadOnlyList<string>> rows)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            return await Task.Run(() =>
            {
                try
                {
                    using (var package = new ExcelPackage())
                    {
                        var safeSheetName = string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName;
                        if (safeSheetName.Length > 31) safeSheetName = safeSheetName.Substring(0, 31);
                        foreach (var invalid in new[] { ':', '\\', '/', '?', '*', '[' , ']' })
                            safeSheetName = safeSheetName.Replace(invalid, ' ');

                        var worksheet = package.Workbook.Worksheets.Add(safeSheetName);

                        for (var i = 0; i < columns.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = columns[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        for (var r = 0; r < rows.Count; r++)
                        {
                            var row = rows[r];
                            for (var c = 0; c < columns.Count; c++)
                            {
                                worksheet.Cells[r + 2, c + 1].Value = c < row.Count ? row[c] : string.Empty;
                            }
                        }

                        if (columns.Count > 0)
                            worksheet.Cells[1, 1, Math.Max(1, rows.Count + 1), columns.Count].AutoFitColumns();

                        _logger.LogInformation("Table Excel file generated with {RowCount} rows and {ColumnCount} columns",
                            rows.Count, columns.Count);
                        return package.GetAsByteArray();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate table Excel file");
                    throw;
                }
            });
        }
    }
}

