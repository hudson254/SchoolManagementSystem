using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Bulk mark import. Each record is validated against the assessment's
    /// maximum and saved through the centralized engine (duplicate prevention
    /// applies per record). Failed records are collected, never silently dropped.
    /// Unit results are recalculated for every successfully imported student.
    /// </summary>
    public class ImportMarksHandler : IRequestHandler<ImportMarksCommand, BulkMarkImportResult>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ImportMarksHandler> _logger;

        public ImportMarksHandler(
            IAssessmentEngine engine,
            IAssessmentRepository assessmentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<ImportMarksHandler> logger)
        {
            _engine = engine;
            _assessmentRepository = assessmentRepository;
            _markRepository = markRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<BulkMarkImportResult> Handle(ImportMarksCommand request, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken);
            if (assessment == null || assessment.IsDeleted)
                throw new NotFoundException("Assessment", request.AssessmentId);

            var result = new BulkMarkImportResult
            {
                TotalRecords = request.Records.Count,
                SuccessCount = 0,
                ErrorCount = 0,
                Errors = new(),
                ImportBatchId = request.ImportBatchId ?? Guid.NewGuid()
            };

            var affectedStudents = new HashSet<Guid>();
            var batchRef = result.ImportBatchId!.ToString();

            foreach (var record in request.Records)
            {
                try
                {
                    if (record.Score < 0 || record.Score > assessment.MaxScore)
                        throw new InvalidOperationException($"Score {record.Score} outside 0..{assessment.MaxScore}.");

                    var mark = await _engine.CalculateAndSaveMarkAsync(
                        request.AssessmentId, record.StudentId, record.Score, cancellationToken);

                    // Tag the persisted mark as bulk-imported.
                    if (mark.EntrySource != MarkEntrySource.BulkImport || mark.ImportBatchReference != batchRef)
                    {
                        mark.EntrySource = MarkEntrySource.BulkImport;
                        mark.ImportBatchReference = batchRef;
                        await _markRepository.UpdateAsync(mark, cancellationToken);
                    }

                    result.SuccessCount++;
                    affectedStudents.Add(record.StudentId);
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Student {record.StudentId}: {ex.Message}");
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Recalculate results for every successfully imported student.
            foreach (var studentId in affectedStudents)
                await _engine.RecalculateAfterGradeChangeAsync(studentId, assessment.UnitId, cancellationToken);

            await _auditService.LogActivityAsync("MarksImported", "StudentAssessmentMark", assessment.Id.ToString(),
                $"Assessment: {assessment.Title}, Success: {result.SuccessCount}, Errors: {result.ErrorCount}, Batch: {batchRef}");

            _logger.LogInformation("Bulk import for assessment {AssessmentId}: {Success} ok, {Errors} failed",
                request.AssessmentId, result.SuccessCount, result.ErrorCount);

            return result;
        }
    }
}
