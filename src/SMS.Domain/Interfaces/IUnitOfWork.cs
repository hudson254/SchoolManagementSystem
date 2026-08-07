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

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
