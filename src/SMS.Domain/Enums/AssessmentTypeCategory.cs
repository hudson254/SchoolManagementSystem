namespace SMS.Domain.Enums
{
    /// <summary>
    /// Built-in assessment type categories. Administrators can add additional
    /// assessment types without modifying source code via the AssessmentType entity.
    /// </summary>
    public enum AssessmentTypeCategory
    {
        Assignment = 1,
        Practical = 2,
        Laboratory = 3,
        Cat = 4,
        Quiz = 5,
        OralExamination = 6,
        Project = 7,
        Presentation = 8,
        FinalExamination = 9,
        SupplementaryExamination = 10,
        RetakeExamination = 11,
        Coursework = 12,
        Participation = 13
    }
}
