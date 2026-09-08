using SMS.Application.DTOs;
using SMS.Domain.Entities;
using System;
using System.Linq;

namespace SMS.Application.Features.Grades
{
    /// <summary>
    /// Maps the authoritative engine-persisted <see cref="UnitResult"/> rows into the
    /// legacy <see cref="GradeDto"/> shape so that every existing grades endpoint
    /// (grades list, unit grades, student grades, exports, transcripts) reads
    /// from the centralized grading engine and no module computes grade points or
    /// grade letters independently.
    ///
    /// Grade points are taken from the value persisted on the UnitResult at calculation
    /// time (preserving the grading-scale version in force historically); the letter
    /// and description come from the engine's band assignment (GradeBands). No
    /// hard-coded boundaries exist anywhere in this mapping.
    /// </summary>
    public static class AuthoritativeGradeMapper
    {
        /// <summary>
        /// Maps a persisted UnitResult into the legacy GradeDto contract.
        /// </summary>
        /// <param name="result">The authoritative unit result (engine-persisted).</param>
        /// <param name="unit">Optional unit; when omitted the result's navigation is used.</param>
        /// <param name="student">Optional student; when omitted the result's navigation is used.</param>
        public static GradeDto MapToGradeDto(UnitResult result, SMS.Domain.Entities.Unit? unit = null, Student? student = null)
        {
            var letter = result.GradeLetter ?? string.Empty;
            return new GradeDto
            {
                Id = result.Id,
                StudentId = result.StudentId,
                EnrollmentId = result.EnrollmentId ?? Guid.Empty,
                GradeValue = string.IsNullOrWhiteSpace(letter) ? null : letter,
                Score = result.FinalPercentage,
                Remarks = string.IsNullOrWhiteSpace(result.GradeDescription) ? null : result.GradeDescription,
                GradedDate = result.PublishedDate ?? result.LastCalculatedDate ?? result.CreatedDate,
                IsPublished = result.IsPublished,
                PublishedDate = result.PublishedDate,
                StudentName = student != null ? BuildName(student) : (result.Student != null ? BuildName(result.Student) : string.Empty),
                StudentNumber = student?.StudentNumber ?? result.Student?.StudentNumber ?? string.Empty,
                UnitName = unit?.Name ?? result.Unit?.Name ?? string.Empty,
                UnitCode = unit?.Code ?? result.Unit?.Code ?? string.Empty,
                Credits = unit?.Credits ?? result.Unit?.Credits ?? 0,
                GradePoints = result.GpaPoints
            };
        }

        private static string BuildName(Student student)
        {
            return $"{student.FirstName} {student.LastName}".Trim();
        }
    }
}