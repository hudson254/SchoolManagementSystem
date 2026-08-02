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
    }
}

