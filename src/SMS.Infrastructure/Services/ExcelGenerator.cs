using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;

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

        public async Task<byte[]> GenerateExcelFromDataAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1")
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(sheetName ?? "Sheet1");

                    var properties = typeof(T).GetProperties();

                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = properties[i].Name;
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    }

                    int row = 2;
                    foreach (var item in data)
                    {
                        for (int col = 0; col < properties.Length; col++)
                        {
                            var value = properties[col].GetValue(item);
                            worksheet.Cells[row, col + 1].Value = value?.ToString();
                        }
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    return await Task.FromResult(package.GetAsByteArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Excel file");
                throw;
            }
        }

        public async Task<byte[]> GenerateStudentReportExcelAsync(object reportData)
        {
            // Convert object to appropriate type if needed
            // For now, return a simple report
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Student Report");

                    worksheet.Cells[1, 1].Value = "Student Number";
                    worksheet.Cells[1, 2].Value = "First Name";
                    worksheet.Cells[1, 3].Value = "Last Name";
                    worksheet.Cells[1, 4].Value = "Email";
                    worksheet.Cells[1, 5].Value = "Programme";
                    worksheet.Cells[1, 6].Value = "Status";

                    for (int i = 1; i <= 6; i++)
                    {
                        worksheet.Cells[1, i].Style.Font.Bold = true;
                    }

                    worksheet.Cells.AutoFitColumns();
                    return await Task.FromResult(package.GetAsByteArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate student report");
                throw;
            }
        }
    }
}
