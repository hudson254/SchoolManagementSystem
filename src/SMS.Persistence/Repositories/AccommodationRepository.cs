using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class AccommodationRepository : BaseRepository<Accommodation>, IAccommodationRepository
    {
        public AccommodationRepository(ApplicationDbContext context, ILogger<AccommodationRepository> logger)
            : base(context, logger)
        {
        }

        // ===== Existing Accommodation Methods =====

        public async Task<IEnumerable<Accommodation>> GetAccommodationsByStudentAsync(Guid studentId)
        {
            return await _dbSet.Where(a => a.StudentId == studentId && !a.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Accommodation>> GetActiveAccommodationsAsync()
        {
            return await _dbSet.Where(a => a.Status == "Active" && !a.IsDeleted).ToListAsync();
        }

        public async Task<Accommodation> GetAccommodationWithDetailsAsync(Guid accommodationId)
        {
            return await _dbSet.Include(a => a.Student).Include(a => a.House).ThenInclude(h => h.Lane)
                .FirstOrDefaultAsync(a => a.Id == accommodationId && !a.IsDeleted);
        }

        public async Task<AccommodationAssignment> GetAssignmentByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AccommodationAssignment>()
                .FirstOrDefaultAsync(a => a.StudentId == studentId && !a.IsDeleted, cancellationToken);
        }

        public async Task<AccommodationAssignment> GetAssignmentWithDetailsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AccommodationAssignment>()
                .Include(a => a.Student)
                .Include(a => a.House)
                .ThenInclude(h => h.Lane)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted, cancellationToken);
        }

        public Task UpdateAssignmentAsync(AccommodationAssignment assignment, CancellationToken cancellationToken = default)
        {
            _context.Set<AccommodationAssignment>().Update(assignment);
            return Task.CompletedTask;
        }

        public async Task<AccommodationAssignment> AddAssignmentAsync(AccommodationAssignment assignment, CancellationToken cancellationToken = default)
        {
            await _context.Set<AccommodationAssignment>().AddAsync(assignment, cancellationToken);
            return assignment;
        }

        public async Task<AccommodationAssignment> GetAssignmentByStudentAndSemesterAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AccommodationAssignment>()
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.SemesterId == semesterId && !a.IsDeleted, cancellationToken);
        }

        // ===== Lane Management =====

        public async Task<Lane> AddLaneAsync(Lane lane, CancellationToken cancellationToken = default)
        {
            await _context.Set<Lane>().AddAsync(lane, cancellationToken);
            return lane;
        }

        public Task UpdateLaneAsync(Lane lane, CancellationToken cancellationToken = default)
        {
            _context.Set<Lane>().Update(lane);
            return Task.CompletedTask;
        }

        public async Task DeleteLaneAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            var lane = await _context.Set<Lane>().FindAsync(new object[] { laneId }, cancellationToken);
            if (lane != null)
            {
                lane.IsDeleted = true;
                _context.Set<Lane>().Update(lane);
            }
        }

        public async Task<Lane?> GetLaneByIdAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Lane>()
                .Include(l => l.Houses)
                .FirstOrDefaultAsync(l => l.Id == laneId && !l.IsDeleted, cancellationToken);
        }

        public async Task<Lane?> GetLaneByNameAsync(string laneName, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Lane>()
                .FirstOrDefaultAsync(l => l.LaneName == laneName && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Lane>> GetLanesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Lane>()
                .Where(l => !l.IsDeleted)
                .Include(l => l.Houses)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Lane> Items, int TotalCount)> GetLanesPagedAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Lane>().Where(l => !l.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(l => l.LaneName.Contains(searchTerm) ||
                                         (l.Description != null && l.Description.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Include(l => l.Houses)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> LaneExistsAsync(string laneName, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Lane>()
                .AnyAsync(l => l.LaneName == laneName && !l.IsDeleted, cancellationToken);
        }

        public async Task<int> CountHousesInLaneAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .CountAsync(h => h.LaneId == laneId && !h.IsDeleted, cancellationToken);
        }

        public async Task<int> CountOccupiedHousesInLaneAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .CountAsync(h => h.LaneId == laneId && h.IsOccupied && !h.IsDeleted, cancellationToken);
        }

        // ===== House Management =====

        public async Task<House> AddHouseAsync(House house, CancellationToken cancellationToken = default)
        {
            await _context.Set<House>().AddAsync(house, cancellationToken);
            return house;
        }

        public async Task<IEnumerable<House>> AddHousesRangeAsync(IEnumerable<House> houses, CancellationToken cancellationToken = default)
        {
            var list = houses.ToList();
            await _context.Set<House>().AddRangeAsync(list, cancellationToken);
            return list;
        }

        public Task UpdateHouseAsync(House house, CancellationToken cancellationToken = default)
        {
            _context.Set<House>().Update(house);
            return Task.CompletedTask;
        }

        public async Task DeleteHouseAsync(Guid houseId, CancellationToken cancellationToken = default)
        {
            var house = await _context.Set<House>().FindAsync(new object[] { houseId }, cancellationToken);
            if (house != null)
            {
                house.IsDeleted = true;
                _context.Set<House>().Update(house);
            }
        }

        public async Task<House?> GetHouseByIdAsync(Guid houseId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .Include(h => h.Lane)
                .Include(h => h.Occupant)
                .FirstOrDefaultAsync(h => h.Id == houseId && !h.IsDeleted, cancellationToken);
        }

        public async Task<House?> GetHouseByNumberAsync(Guid laneId, string houseNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .FirstOrDefaultAsync(h => h.LaneId == laneId && h.HouseNumber == houseNumber && !h.IsDeleted, cancellationToken);
        }

        public async Task<(IEnumerable<House> Items, int TotalCount)> GetHousesPagedAsync(int page, int pageSize, Guid? laneId, string? searchTerm, string? status, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<House>().Where(h => !h.IsDeleted);

            if (laneId.HasValue)
            {
                query = query.Where(h => h.LaneId == laneId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(h => h.HouseNumber.Contains(searchTerm) ||
                                         (h.Notes != null && h.Notes.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(h => h.Status == status);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Include(h => h.Lane)
                .Include(h => h.Occupant)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IEnumerable<House>> GetHousesByLaneAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .Where(h => h.LaneId == laneId && !h.IsDeleted)
                .OrderBy(h => h.HouseNumberNumeric)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<House>> GetAvailableHousesAsync(Guid? laneId, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<House>()
                .Where(h => !h.IsOccupied && h.IsAvailable && h.IsEnabled && !h.IsDeleted);

            if (laneId.HasValue)
            {
                query = query.Where(h => h.LaneId == laneId.Value);
            }

            return await query
                .Include(h => h.Lane)
                .OrderBy(h => h.LaneId)
                .ThenBy(h => h.HouseNumberNumeric)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HouseExistsInLaneAsync(Guid laneId, string houseNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .AnyAsync(h => h.LaneId == laneId && h.HouseNumber == houseNumber && !h.IsDeleted, cancellationToken);
        }

        public async Task<int> GetNextHouseNumberSequenceAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            var maxNumber = await _context.Set<House>()
                .Where(h => h.LaneId == laneId && !h.IsDeleted)
                .MaxAsync(h => (int?)h.HouseNumberNumeric, cancellationToken) ?? 0;
            return maxNumber + 1;
        }

        // ===== Occupancy & Reports =====

        public async Task<(int Total, int Occupied, int Vacant, int Maintenance, int Disabled, int Reserved)> GetLaneOccupancySummaryAsync(Guid laneId, CancellationToken cancellationToken = default)
        {
            var houses = await _context.Set<House>()
                .Where(h => h.LaneId == laneId && !h.IsDeleted)
                .ToListAsync(cancellationToken);

            var total = houses.Count;
            var occupied = houses.Count(h => h.Status == HouseStatus.Occupied);
            var maintenance = houses.Count(h => h.Status == HouseStatus.Maintenance);
            var disabled = houses.Count(h => h.Status == HouseStatus.Disabled);
            var reserved = houses.Count(h => h.Status == HouseStatus.Reserved);
            var vacant = houses.Count(h => h.Status == HouseStatus.Vacant);

            return (total, occupied, vacant, maintenance, disabled, reserved);
        }

        public async Task<(int Total, int Occupied, int Vacant, int Maintenance, int Disabled)> GetOverallOccupancySummaryAsync(CancellationToken cancellationToken = default)
        {
            var houses = await _context.Set<House>()
                .Where(h => !h.IsDeleted)
                .ToListAsync(cancellationToken);

            var total = houses.Count;
            var occupied = houses.Count(h => h.Status == HouseStatus.Occupied);
            var maintenance = houses.Count(h => h.Status == HouseStatus.Maintenance);
            var disabled = houses.Count(h => h.Status == HouseStatus.Disabled);
            var vacant = houses.Count(h => h.Status == HouseStatus.Vacant);

            return (total, occupied, vacant, maintenance, disabled);
        }

        public async Task<IEnumerable<House>> GetHousesByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .Where(h => h.Status == status && !h.IsDeleted)
                .Include(h => h.Lane)
                .Include(h => h.Occupant)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<House>> GetHousesUnderMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<House>()
                .Where(h => h.Status == HouseStatus.Maintenance && !h.IsDeleted)
                .Include(h => h.Lane)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AccommodationAssignment>> GetAssignmentsWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<AccommodationAssignment>()
                .Include(a => a.Student)
                .Include(a => a.House)
                    .ThenInclude(h => h.Lane)
                .Where(a => !a.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AccommodationAssignment>> GetAssignmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AccommodationAssignment>()
                .Where(a => a.StudentId == studentId && !a.IsDeleted)
                .Include(a => a.House)
                    .ThenInclude(h => h.Lane)
                .ToListAsync(cancellationToken);
        }

        // ===== Legacy methods (kept for backward compatibility) =====

        public async Task<IEnumerable<Room>> GetRoomsAsync(int page, int pageSize, string? searchTerm, string? roomType, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Room>().Where(r => !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r => r.RoomNumber.Contains(searchTerm));
            if (!string.IsNullOrWhiteSpace(roomType))
                query = query.Where(r => r.RoomType == roomType);

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }

        public async Task<int> CountRoomsAsync(string? searchTerm, string? roomType, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Room>().Where(r => !r.IsDeleted);
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(r => r.RoomNumber.Contains(searchTerm));
            if (!string.IsNullOrWhiteSpace(roomType))
                query = query.Where(r => r.RoomType == roomType);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Room> GetRoomWithDetailsAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Room>()
                .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Room>()
                .Where(r => r.OccupiedCount < r.Capacity && !r.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetRoomsByBuildingAsync(string building, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Room>()
                .Where(r => r.Block.Building == building && !r.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Room>().Where(r => !r.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<Building> AddBuildingAsync(Building building, CancellationToken cancellationToken = default)
        {
            await _context.Set<Building>().AddAsync(building, cancellationToken);
            return building;
        }

        public async Task<Building?> GetBuildingByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Building>()
                .FirstOrDefaultAsync(b => b.Name == code && !b.IsDeleted, cancellationToken);
        }

        public async Task<Building?> GetBuildingByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Building>()
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Building>()
                .Where(b => !b.IsDeleted)
                .Include(b => b.Blocks)
                .ToListAsync(cancellationToken);
        }
    }
}


