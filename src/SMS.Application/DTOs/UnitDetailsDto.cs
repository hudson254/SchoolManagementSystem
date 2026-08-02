using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class UnitDetailsDto : UnitDto
    {
        public string? AssessmentMethods { get; set; }
        public string? RecommendedTextbooks { get; set; }
        public string? LearningOutcomes { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? InstructorName { get; set; }
        public int EnrolledStudents { get; set; }
        public List<AssignmentSummaryDto> Assignments { get; set; } = new List<AssignmentSummaryDto>();
    }

    public class AssignmentSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}