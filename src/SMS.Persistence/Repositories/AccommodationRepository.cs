using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class AccommodationRepository : IAccommodationRepository
    {
        private readonly ApplicationDbContext _context;

        public AccommodationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Room?> GetRoomByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Rooms
                .Include(r => r.Block)
                    .ThenInclude(b => b.Building)
                .Include(r => r.CurrentAssignment)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        }

        public async Task<Room?> GetRoomWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Rooms
                .Include(r => r.Block)
                    .ThenInclude(b => b.Building)
                .Include(r => r.AssignmentHistory)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.User)
                .Include(r => r.AssignmentHistory)
                    .ThenInclude(a => a.Semester)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetRoomsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            Guid? buildingId,
            Guid? blockId,
            bool? isAvailable,
            bool? isOccupied,
            string? roomType,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Rooms
                .Include(r => r.Block)
                    .ThenInclude(b => b.Building)
                .Include(r => r.CurrentAssignment)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.User)
                .Where(r => !r.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    r.RoomNumber.Contains(searchTerm) ||
                    r.Block.Name.Contains(searchTerm) ||
                    r.Block.Building.Name.Contains(searchTerm));
            }

            if (buildingId.HasValue)
                query = query.Where(r => r.Block.BuildingId == buildingId.Value);

            if (blockId.HasValue)
                query = query.Where(r => r.BlockId == blockId.Value);

            if (isAvailable.HasValue)
                query = query.Where(r => r.IsAvailable == isAvailable.Value);

            if (isOccupied.HasValue)
                query = query.Where(r => r.IsOccupied == isOccupied.Value);

            if (!string.IsNullOrEmpty(roomType))
                query = query.Where(r => r.RoomType == roomType);

            query = sortDescending
                ? query.OrderByDescending(GetRoomSortExpression(sortBy))
                : query.OrderBy(GetRoomSortExpression(sortBy));

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountRoomsAsync(
            string? searchTerm,
            Guid? buildingId,
            Guid? blockId,
            bool? isAvailable,
            bool? isOccupied,
            string? roomType,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Rooms.Where(r => !r.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    r.RoomNumber.Contains(searchTerm) ||
                    r.Block.Name.Contains(searchTerm) ||
                    r.Block.Building.Name.Contains(searchTerm));
            }

            if (buildingId.HasValue)
                query = query.Where(r => r.Block.BuildingId == buildingId.Value);

            if (blockId.HasValue)
                query = query.Where(r => r.BlockId == blockId.Value);

            if (isAvailable.HasValue)
                query = query.Where(r => r.IsAvailable == isAvailable.Value);

            if (isOccupied.HasValue)
                query = query.Where(r => r.IsOccupied == isOccupied.Value);

            if (!string.IsNullOrEmpty(roomType))
                query = query.Where(r => r.RoomType == roomType);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(Guid? buildingId, Guid? blockId, string? roomType, CancellationToken cancellationToken = default)
        {
            var query = _context.Rooms
                .Include(r => r.Block)
                    .ThenInclude(b => b.Building)
                .Where(r => r.IsAvailable && !r.IsOccupied && !r.IsDeleted);

            if (buildingId.HasValue)
                query = query.Where(r => r.Block.BuildingId == buildingId.Value);

            if (blockId.HasValue)
                query = query.Where(r => r.BlockId == blockId.Value);

            if (!string.IsNullOrEmpty(roomType))
                query = query.Where(r => r.RoomType == roomType);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetRoomsByBuildingAsync(Guid? buildingId, CancellationToken cancellationToken = default)
        {
            var query = _context.Rooms
                .Include(r => r.Block)
                    .ThenInclude(b => b.Building)
                .Where(r => !r.IsDeleted);

            if (buildingId.HasValue)
                query = query.Where(r => r.Block.BuildingId == buildingId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Rooms
                .Include(r => r.Block)
                    .ThenInclude(b => b.Building)
                .Where(r => !r.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<AccommodationAssignment?> GetAssignmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.AccommodationAssignments
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.Building)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }

        public async Task<AccommodationAssignment?> GetAssignmentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.AccommodationAssignments
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.Building)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<AccommodationAssignment>> GetAssignmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _context.AccommodationAssignments
                .Include(a => a.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.Building)
                .Include(a => a.Semester)
                .Where(a => a.StudentId == studentId && !a.IsDeleted)
                .OrderByDescending(a => a.AssignmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<AccommodationAssignment?> GetAssignmentByStudentAndSemesterAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.AccommodationAssignments
                .Include(a => a.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.Building)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a =>
                    a.StudentId == studentId &&
                    a.SemesterId == semesterId &&
                    !a.IsDeleted, cancellationToken);
        }

        public async Task<AccommodationAssignment?> GetAssignmentByStudentAsync(Guid studentId, Guid? semesterId, CancellationToken cancellationToken = default)
        {
            var query = _context.AccommodationAssignments
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.Building)
                .Include(a => a.Semester)
                .Where(a => a.StudentId == studentId && !a.IsDeleted);

            if (semesterId.HasValue)
                query = query.Where(a => a.SemesterId == semesterId.Value);

            return await query
                .OrderByDescending(a => a.AssignmentDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AccommodationAssignment> AddAssignmentAsync(AccommodationAssignment assignment, CancellationToken cancellationToken = default)
        {
            await _context.AccommodationAssignments.AddAsync(assignment, cancellationToken);

            // Update room status
            var room = await _context.Rooms.FindAsync(assignment.RoomId);
            if (room != null)
            {
                room.IsAvailable = false;
                room.IsOccupied = true;
                room.Status = "Occupied";
            }

            return assignment;
        }

        public Task UpdateAssignmentAsync(AccommodationAssignment assignment, CancellationToken cancellationToken = default)
        {
            _context.AccommodationAssignments.Update(assignment);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Buildings
                .Include(b => b.Blocks)
                    .ThenInclude(bl => bl.Rooms)
                .Where(b => b.IsActive && !b.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<Building?> GetBuildingWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Buildings
                .Include(b => b.Blocks)
                    .ThenInclude(bl => bl.Rooms)
                        .ThenInclude(r => r.CurrentAssignment)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);
        }

        private static Expression<Func<Room, object>> GetRoomSortExpression(string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "roomnumber" => r => r.RoomNumber,
                "capacity" => r => r.Capacity,
                "price" => r => r.PricePerSemester,
                "createddate" => r => r.CreatedDate,
                _ => r => r.RoomNumber
            };
        }
    }
}