using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IExcelGenerator
    {
        Task<byte[]> GenerateExcelFromDataAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1");
        Task<byte[]> GenerateStudentReportExcelAsync(object reportData);

        /// <summary>
        /// Renders an Excel-compatible spreadsheet from explicit display columns
        /// and display-ready rows (used by report exports so headers read like a
        /// report rather than raw property names).
        /// </summary>
        Task<byte[]> GenerateTableExcelAsync(
            string sheetName,
            IReadOnlyList<string> columns,
            IReadOnlyList<IReadOnlyList<string>> rows);
    }
}
