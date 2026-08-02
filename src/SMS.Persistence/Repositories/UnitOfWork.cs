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

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
