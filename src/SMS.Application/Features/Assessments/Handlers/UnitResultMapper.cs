using SMS.Application.Features.Assessments.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Maps engine-persisted UnitResult data (the single authoritative grading
    /// source) into the student result DTO. No independent grade math is computed
    /// here; pass state is derived from the configured certificate rule threshold
    /// and grade colours from the active grading scale bands.
    /// </summary>
    internal class UnitResultMapper
    {
        internal static async Task<StudentResultDto> MapAsync(
            UnitResult result,
            SMS.Domain.Entities.Unit? unit,
            IStudentRepository studentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentRepository assessmentRepository,
            IGradingScaleRepository gradingScaleRepository,
            ICertificateRuleRepository certificateRuleRepository,
            bool onlyPublishedMarks,
            CancellationToken ct)
        {
            var dto = new StudentResultDto
            {
                StudentId = result.StudentId,
                UnitId = result.UnitId,
                UnitName = unit?.Name ?? string.Empty,
                FinalScore = result.FinalPercentage,
                FinalGrade = result.GradeLetter,
                GradeDescription = result.GradeDescription,
                GradeColor = string.Empty,
                IsPassed = false,
                GradingScaleVersionId = result.GradingScaleVersionId,
                IsPublished = result.IsPublished,
                PublicationStatus = result.PublicationStatus,
                AssessmentMarks = new()
            };

            if (result.Student != null)
                dto.StudentName = $"{result.Student.FirstName} {result.Student.LastName}".Trim();
            else if (unit == null && result.StudentId == null)
                dto.StudentName = string.Empty;
            else
            {
                var student = await studentRepository.GetByIdAsync(result.StudentId, ct);
                if (student != null)
                    dto.StudentName = $"{student.FirstName} {student.LastName}".Trim();
            }

            // Pass threshold from the configured institutional certificate rule.
            var rule = await certificateRuleRepository.GetActiveRuleAsync(ct);
            var minPass = rule?.MinimumPassingPercentage ?? 50m;
            dto.IsPassed = result.FinalPercentage >= minPass;

            // Grade colour from the active grading scale (historical results keep
            // the version recorded on GradingScaleVersionId).
            var scale = await gradingScaleRepository.GetActiveVersionAsync(ct);
            if (scale != null)
            {
                var band = scale.Bands
                    ?.OrderByDescending(b => b.MinPercentage)
                    .FirstOrDefault(b => result.FinalPercentage >= b.MinPercentage && result.FinalPercentage <= b.MaxPercentage);
                if (band != null)
                    dto.GradeColor = band.ColorCode;
            }

            // Assessment-level breakdown (marks visible to the audience).
            var assessments = (await assessmentRepository.GetByUnitAsync(result.UnitId, ct))
                .Where(a => !a.IsDeleted)
                .ToList();
            var marks = (await markRepository.GetByUnitAndStudentAsync(result.UnitId, result.StudentId, ct))
                .Where(m => !m.IsDraft)
                .ToList();

            foreach (var mark in marks)
            {
                var assessment = assessments.FirstOrDefault(a => a.Id == mark.AssessmentId);
                if (assessment == null)
                    continue;
                if (onlyPublishedMarks && assessment.PublicationStatus != SMS.Domain.Enums.ResultPublicationStatus.Published)
                    continue;
                dto.AssessmentMarks.Add(EnterMarkHandler.Map(mark, assessment));
            }

            return dto;
        }
    }
}