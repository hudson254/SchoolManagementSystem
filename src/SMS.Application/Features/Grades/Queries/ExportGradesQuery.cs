using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Exceptions;
using SMS.Application.Features.Grades;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Grades.Queries
{
    public class ExportGradesQuery : IRequest<ReportFileResult>
    {
        public Guid UnitId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    /// <summary>
    /// Excel export of grades for a unit, backed exclusively by the authoritative
    /// engine data (UnitResults(. The exported rows match exactly what the grades
    /// UI shows for the same filters (no independent calculation, no hard-coded bands).
    /// </summary>
    public class ExportGradesQueryHandler : IRequestHandler<ExportGradesQuery, ReportFileResult>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IExcelGenerator _excelGenerator;
        private readonly ILogger<ExportGradesQueryHandler> _logger;

        public ExportGradesQueryHandler(
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            IExcelGenerator excelGenerator,
            ILogger<ExportGradesQueryHandler> logger)
        {
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _excelGenerator = excelGenerator;
            _logger = logger;
        }

        public async Task<ReportFileResult> Handle(ExportGradesQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null) throw new NotFoundException("Unit", request.UnitId);

            var results = (await _unitResultRepository.GetByUnitAsync(request.UnitId, cancellationToken)).ToList();

            if (request.SemesterId.HasValue)
                results = results.Where(r => r.SemesterId == request.SemesterId.Value).ToList();

            var exportData = results.Select(r => new
            {
                StudentNumber = r.Student?.StudentNumber ?? "",
                StudentName = r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : "",
                Grade = r.GradeLetter ?? "",
                Score = r.FinalPercentage,
                Remarks = string.IsNullOrWhiteSpace(r.GradeDescription) ? null : r.GradeDescription,
                Published = r.IsPublished ? "Yes" : "No"
            }).ToList();

            var fileName = $"Grades_{unit.Code}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            var fileContent = await _excelGenerator.GenerateExcelFromDataAsync(exportData, "Grades");

            return new ReportFileResult
            {
                FileContent = fileContent,
                FileName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
    }
}