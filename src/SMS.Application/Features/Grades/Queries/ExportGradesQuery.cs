using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Queries
{
    public class ExportGradesQuery : IRequest<ReportFileResult>
    {
        public Guid UnitId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class ExportGradesQueryHandler : IRequestHandler<ExportGradesQuery, ReportFileResult>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IExcelGenerator _excelGenerator;
        private readonly ILogger<ExportGradesQueryHandler> _logger;

        public ExportGradesQueryHandler(
            IGradeRepository gradeRepository,
            IUnitRepository unitRepository,
            IExcelGenerator excelGenerator,
            ILogger<ExportGradesQueryHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _unitRepository = unitRepository;
            _excelGenerator = excelGenerator;
            _logger = logger;
        }

        public async Task<ReportFileResult> Handle(ExportGradesQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
                throw new NotFoundException("Unit", request.UnitId);

            var grades = await _gradeRepository.GetGradesByUnitAsync(request.UnitId);

            if (request.SemesterId.HasValue)
                grades = grades.Where(g => g.SemesterId == request.SemesterId.Value);

            var exportData = grades.Select(g => new
            {
                StudentNumber = g.Student?.StudentNumber ?? "",
                StudentName = g.Student != null ? $"{g.Student.FirstName} {g.Student.LastName}" : "",
                Grade = g.GradeValue ?? "",
                Score = g.Score,
                Remarks = g.Remarks ?? "",
                Published = g.IsPublished ? "Yes" : "No"
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
