using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class LecturerRepository : ILecturerRepository
    {
        private readonly ApplicationDbContext _context;

        public LecturerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Lecturer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.UnitAllocations)
                    .ThenInclude(u => u.Unit)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => !l.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> FindAsync(Expression<Func<Lecturer, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => !l.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Lecturer> AddAsync(Lecturer entity, CancellationToken cancellationToken = default)
        {
            await _context.Lecturers.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Lecturer>> AddRangeAsync(IEnumerable<Lecturer> entities, CancellationToken cancellationToken = default)
        {
            await _context.Lecturers.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Lecturer entity, CancellationToken cancellationToken = default)
        {
            _context.Lecturers.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Lecturer entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Lecturers.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Lecturer, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Lecturer, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Lecturers.Where(l => !l.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Lecturer?> GetLecturerWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.UnitAllocations)
                    .ThenInclude(u => u.Unit)
                .Include(l => l.UnitAllocations)
                    .ThenInclude(u => u.Semester)
                .Include(l => l.Assignments)
                .Include(l => l.LectureNotes)
                .Include(l => l.Classes)
                .Include(l => l.AccommodationAssignment)
                    .ThenInclude(a => a.Room)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> GetLecturersByUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => l.UnitAllocations.Any(u => u.UnitId == unitId && u.Status == "Active") && !l.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> GetLecturersBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => l.UnitAllocations.Any(u => u.SemesterId == semesterId) && !l.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<Lecturer?> GetLecturerByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.EmployeeNumber == employeeNumber && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> GetVerifiedLecturersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => l.IsVerified && l.IsActive && !l.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> GetUnverifiedLecturersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => !l.IsVerified && !l.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Lecturer>> GetActiveLecturersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Lecturers
                .Include(l => l.User)
                .Where(l => l.IsActive && !l.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountLecturersAsync(string? searchTerm, bool? isVerified, bool? isActive, CancellationToken cancellationToken = default)
        {
            var query = _context.Lecturers.Where(l => !l.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l =>
                    l.EmployeeNumber.Contains(searchTerm) ||
                    l.User.FirstName.Contains(searchTerm) ||
                    l.User.LastName.Contains(searchTerm) ||
                    l.User.Email.Contains(searchTerm));
            }

            if (isVerified.HasValue)
                query = query.Where(l => l.IsVerified == isVerified.Value);

            if (isActive.HasValue)
                query = query.Where(l => l.IsActive == isActive.Value);

            return await query.CountAsync(cancellationToken);
        }
    }
}