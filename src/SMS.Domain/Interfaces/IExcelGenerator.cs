using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IExcelGenerator
    {
        Task<byte[]> GenerateExcelFromDataAsync<T>(IEnumerable<T> data, string sheetName = "Sheet1");
        Task<byte[]> GenerateStudentReportExcelAsync(object reportData);
    }
}