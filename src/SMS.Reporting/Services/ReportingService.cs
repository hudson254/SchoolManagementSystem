using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Reporting.Services
{
    public interface IReportingService
    {
        Task<byte[]> GenerateStudentReportAsync(Guid studentId);
        Task<byte[]> GenerateAttendanceReportAsync(Guid? courseId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateGradeReportAsync(Guid? courseId, Guid? semesterId);
        Task<byte[]> GenerateFinanceReportAsync(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateDashboardAnalyticsAsync();
        Task<byte[]> ExportStudentListAsync();
        Task<byte[]> ExportCourseListAsync();
    }

    public class ReportingService : IReportingService
    {
        private readonly ILogger<ReportingService> _logger;
        private readonly IPdfGenerator _pdfGenerator;
        private readonly IExcelGenerator _excelGenerator;
        private readonly ICsvService _csvService;

        public ReportingService(
            ILogger<ReportingService> logger,
            IPdfGenerator pdfGenerator,
            IExcelGenerator excelGenerator,
            ICsvService csvService)
        {
            _logger = logger;
            _pdfGenerator = pdfGenerator;
            _excelGenerator = excelGenerator;
            _csvService = csvService;
        }

        public async Task<byte[]> GenerateStudentReportAsync(Guid studentId)
        {
            try
            {
                var reportData = new
                {
                    StudentId = studentId,
                    GeneratedDate = DateTime.UtcNow,
                    Type = "Student Report"
                };

                var pdf = await _pdfGenerator.GenerateTranscriptPdfAsync(reportData);
                _logger.LogInformation("Student report generated for student {StudentId}", studentId);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate student report for {StudentId}", studentId);
                throw;
            }
        }

        public async Task<byte[]> GenerateAttendanceReportAsync(Guid? courseId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var reportData = new
                {
                    CourseId = courseId,
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedDate = DateTime.UtcNow,
                    Type = "Attendance Report"
                };

                var pdf = await _pdfGenerator.GenerateReportPdfAsync(reportData);
                _logger.LogInformation("Attendance report generated for course {CourseId}", courseId);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate attendance report");
                throw;
            }
        }

        public async Task<byte[]> GenerateGradeReportAsync(Guid? courseId, Guid? semesterId)
        {
            try
            {
                var reportData = new
                {
                    CourseId = courseId,
                    SemesterId = semesterId,
                    GeneratedDate = DateTime.UtcNow,
                    Type = "Grade Report"
                };

                var pdf = await _pdfGenerator.GenerateReportPdfAsync(reportData);
                _logger.LogInformation("Grade report generated for course {CourseId}, semester {SemesterId}", courseId, semesterId);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate grade report");
                throw;
            }
        }

        public async Task<byte[]> GenerateFinanceReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var reportData = new
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedDate = DateTime.UtcNow,
                    Type = "Finance Report"
                };

                var pdf = await _pdfGenerator.GenerateReportPdfAsync(reportData);
                _logger.LogInformation("Finance report generated from {StartDate} to {EndDate}", startDate, endDate);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate finance report");
                throw;
            }
        }

        public async Task<byte[]> GenerateDashboardAnalyticsAsync()
        {
            try
            {
                var analyticsData = new
                {
                    GeneratedDate = DateTime.UtcNow,
                    TotalStudents = 0,
                    TotalLecturers = 0,
                    TotalCourses = 0,
                    ActiveEnrollments = 0,
                    Type = "Dashboard Analytics"
                };

                var pdf = await _pdfGenerator.GenerateReportPdfAsync(analyticsData);
                _logger.LogInformation("Dashboard analytics generated");
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate dashboard analytics");
                throw;
            }
        }

        public async Task<byte[]> ExportStudentListAsync()
        {
            try
            {
                var students = new List<object>();
                return await _excelGenerator.GenerateExcelFromDataAsync(students, "Students");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export student list");
                throw;
            }
        }

        public async Task<byte[]> ExportCourseListAsync()
        {
            try
            {
                var courses = new List<object>();
                return await _excelGenerator.GenerateExcelFromDataAsync(courses, "Courses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export course list");
                throw;
            }
        }
    }
}
