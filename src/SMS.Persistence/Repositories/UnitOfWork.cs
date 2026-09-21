using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;

namespace SMS.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private IDbContextTransaction _transaction;

        private IStudentRepository _students;
        private ICourseRepository _courses;
        private IUnitRepository _units;
        private IEnrollmentRepository _enrollments;
        private IGradeRepository _grades;
        private IAssignmentRepository _assignments;
        private IAccommodationRepository _accommodations;
        private IAttendanceRepository _attendances;
        private ITimetableRepository _timetables;
        private ILecturerRepository _lecturers;
        private IDepartmentRepository _departments;
        private ICalendarEventRepository _calendarEvents;
        private ICourseOfferingRepository _courseOfferings;
        private ICourseOfferingUnitRepository _courseOfferingUnits;
        private ICourseOfferingEnrollmentRepository _courseOfferingEnrollments;
        private ICourseOfferingLecturerRepository _courseOfferingLecturers;
        private IAssignmentIssueReportRepository _assignmentIssueReports;
        private IAssessmentRepository _assessments;
        private IStudentAssessmentMarkRepository _studentAssessmentMarks;
        private IAssessmentTypeRepository _assessmentTypes;
        private IAssessmentTemplateRepository _assessmentTemplates;
        private IGradingScaleRepository _gradingScales;
        private IGradeBandRepository _gradeBands;
        private ICertificateRuleRepository _certificateRules;
        private IStudentCertificateEligibilityRepository _studentCertificateEligibilities;
        private IGradeChangeHistoryRepository _gradeChangeHistories;
        private IUnitResultRepository _unitResults;
        private IModerationRecordRepository _moderationRecords;
        private IAssessmentExemptionRepository _assessmentExemptions;
        private IOrderRepository _orders;
        private IRoadAccountRepository _roadAccounts;
        private IOrderImportRepository _orderImports;


        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public IStudentRepository Students =>
            _students ??= new StudentRepository(_context, _loggerFactory.CreateLogger<StudentRepository>());

        public ICourseRepository Courses =>
            _courses ??= new CourseRepository(_context, _loggerFactory.CreateLogger<CourseRepository>());

        public IUnitRepository Units =>
            _units ??= new UnitRepository(_context, _loggerFactory.CreateLogger<UnitRepository>());

        public IEnrollmentRepository Enrollments =>
            _enrollments ??= new EnrollmentRepository(_context, _loggerFactory.CreateLogger<EnrollmentRepository>());

        public IGradeRepository Grades =>
            _grades ??= new GradeRepository(_context, _loggerFactory.CreateLogger<GradeRepository>());

        public IAssignmentRepository Assignments =>
            _assignments ??= new AssignmentRepository(_context, _loggerFactory.CreateLogger<AssignmentRepository>());

        public IAccommodationRepository Accommodations =>
            _accommodations ??= new AccommodationRepository(_context, _loggerFactory.CreateLogger<AccommodationRepository>());

        public IAttendanceRepository Attendances =>
            _attendances ??= new AttendanceRepository(_context, _loggerFactory.CreateLogger<AttendanceRepository>());

        public ITimetableRepository Timetables =>
            _timetables ??= new TimetableRepository(_context, _loggerFactory.CreateLogger<TimetableRepository>());

        public ILecturerRepository Lecturers =>
            _lecturers ??= new LecturerRepository(_context, _loggerFactory.CreateLogger<LecturerRepository>());

        public IDepartmentRepository Departments =>
            _departments ??= new DepartmentRepository(_context, _loggerFactory.CreateLogger<DepartmentRepository>());

        public ICalendarEventRepository CalendarEvents =>
            _calendarEvents ??= new CalendarEventRepository(_context, _loggerFactory.CreateLogger<CalendarEventRepository>());

        public ICourseOfferingRepository CourseOfferings =>
            _courseOfferings ??= new CourseOfferingRepository(_context, _loggerFactory.CreateLogger<CourseOfferingRepository>());

        public ICourseOfferingUnitRepository CourseOfferingUnits =>
            _courseOfferingUnits ??= new CourseOfferingUnitRepository(_context, _loggerFactory.CreateLogger<CourseOfferingUnitRepository>());

        public ICourseOfferingEnrollmentRepository CourseOfferingEnrollments =>
            _courseOfferingEnrollments ??= new CourseOfferingEnrollmentRepository(_context, _loggerFactory.CreateLogger<CourseOfferingEnrollmentRepository>());

        public ICourseOfferingLecturerRepository CourseOfferingLecturers =>
            _courseOfferingLecturers ??= new CourseOfferingLecturerRepository(_context, _loggerFactory.CreateLogger<CourseOfferingLecturerRepository>());

        public IAssignmentIssueReportRepository AssignmentIssueReports =>
            _assignmentIssueReports ??= new AssignmentIssueReportRepository(_context, _loggerFactory.CreateLogger<AssignmentIssueReportRepository>());

        public IAssessmentRepository Assessments =>
            _assessments ??= new AssessmentRepository(_context, _loggerFactory.CreateLogger<AssessmentRepository>());

        public IStudentAssessmentMarkRepository StudentAssessmentMarks =>
            _studentAssessmentMarks ??= new StudentAssessmentMarkRepository(_context, _loggerFactory.CreateLogger<StudentAssessmentMarkRepository>());

        public IAssessmentTypeRepository AssessmentTypes =>
            _assessmentTypes ??= new AssessmentTypeRepository(_context, _loggerFactory.CreateLogger<AssessmentTypeRepository>());

        public IAssessmentTemplateRepository AssessmentTemplates =>
            _assessmentTemplates ??= new AssessmentTemplateRepository(_context, _loggerFactory.CreateLogger<AssessmentTemplateRepository>());

        public IGradingScaleRepository GradingScales =>
            _gradingScales ??= new GradingScaleRepository(_context, _loggerFactory.CreateLogger<GradingScaleRepository>());

        public IGradeBandRepository GradeBands =>
            _gradeBands ??= new GradeBandRepository(_context, _loggerFactory.CreateLogger<GradeBandRepository>());

        public ICertificateRuleRepository CertificateRules =>
            _certificateRules ??= new CertificateRuleRepository(_context, _loggerFactory.CreateLogger<CertificateRuleRepository>());

        public IStudentCertificateEligibilityRepository StudentCertificateEligibilities =>
            _studentCertificateEligibilities ??= new StudentCertificateEligibilityRepository(_context, _loggerFactory.CreateLogger<StudentCertificateEligibilityRepository>());

        public IGradeChangeHistoryRepository GradeChangeHistories =>
            _gradeChangeHistories ??= new GradeChangeHistoryRepository(_context, _loggerFactory.CreateLogger<GradeChangeHistoryRepository>());

        public IUnitResultRepository UnitResults =>
            _unitResults ??= new UnitResultRepository(_context, _loggerFactory.CreateLogger<UnitResultRepository>());

        public IModerationRecordRepository ModerationRecords =>
            _moderationRecords ??= new ModerationRecordRepository(_context, _loggerFactory.CreateLogger<ModerationRecordRepository>());

        public IAssessmentExemptionRepository AssessmentExemptions =>
            _assessmentExemptions ??= new AssessmentExemptionRepository(_context, _loggerFactory.CreateLogger<AssessmentExemptionRepository>());

        public IOrderRepository Orders =>
            _orders ??= new OrderRepository(_context, _loggerFactory.CreateLogger<OrderRepository>());

        public IRoadAccountRepository RoadAccounts =>
            _roadAccounts ??= new RoadAccountRepository(_context, _loggerFactory.CreateLogger<RoadAccountRepository>());

        public IOrderImportRepository OrderImports =>
            _orderImports ??= new OrderImportRepository(_context, _loggerFactory.CreateLogger<OrderImportRepository>());

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <inheritdoc />
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            if (!strategy.RetriesOnFailure)
            {
                return await ExecuteInTransactionCoreAsync(operation, cancellationToken);
            }

            // Retrying execution strategies (Npgsql EnableRetryOnFailure) forbid
            // user-initiated transactions unless the execution strategy itself
            // executes the whole unit (EF Core requirement).
            return await strategy.ExecuteAsync(
                () => ExecuteInTransactionCoreAsync(operation, cancellationToken));
        }

        /// <inheritdoc />
        public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteInTransactionAsync<bool>(
                async () =>
                {
                    await operation();
                    return true;
                },
                cancellationToken);
        }

        private async Task<T> ExecuteInTransactionCoreAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
