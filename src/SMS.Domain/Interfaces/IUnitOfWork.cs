using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IStudentRepository Students { get; }
        ICourseRepository Courses { get; }
        IUnitRepository Units { get; }
        IEnrollmentRepository Enrollments { get; }
        IGradeRepository Grades { get; }
        IAssignmentRepository Assignments { get; }
        IAccommodationRepository Accommodations { get; }
        IAttendanceRepository Attendances { get; }
        ITimetableRepository Timetables { get; }
        ILecturerRepository Lecturers { get; }
        IDepartmentRepository Departments { get; }
        ICalendarEventRepository CalendarEvents { get; }
        ICourseOfferingRepository CourseOfferings { get; }
        ICourseOfferingUnitRepository CourseOfferingUnits { get; }
        ICourseOfferingEnrollmentRepository CourseOfferingEnrollments { get; }
        ICourseOfferingLecturerRepository CourseOfferingLecturers { get; }
        IAssignmentIssueReportRepository AssignmentIssueReports { get; }
        IAssessmentRepository Assessments { get; }
        IStudentAssessmentMarkRepository StudentAssessmentMarks { get; }
        IAssessmentTypeRepository AssessmentTypes { get; }
        IAssessmentTemplateRepository AssessmentTemplates { get; }
        IGradingScaleRepository GradingScales { get; }
        IGradeBandRepository GradeBands { get; }
        ICertificateRuleRepository CertificateRules { get; }
        IStudentCertificateEligibilityRepository StudentCertificateEligibilities { get; }
        IGradeChangeHistoryRepository GradeChangeHistories { get; }
        IUnitResultRepository UnitResults { get; }
        IModerationRecordRepository ModerationRecords { get; }
        IAssessmentExemptionRepository AssessmentExemptions { get; }

        // OMS repositories (Phase 2A)
        IOrderRepository Orders { get; }
        IRoadAccountRepository RoadAccounts { get; }
        IOrderImportRepository OrderImports { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the operation as one atomic unit inside a database transaction.
        /// Compatible with retrying execution strategies (Npgsql EnableRetryOnFailure):
        /// when retries are enabled, the whole unit is executed by the execution
        /// strategy itself, as EF Core requires for user-initiated transactions.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>Non-generic variant of <see cref="ExecuteInTransactionAsync{T}(Func{Task{T}}, CancellationToken)"/>.</summary>
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    }
}

