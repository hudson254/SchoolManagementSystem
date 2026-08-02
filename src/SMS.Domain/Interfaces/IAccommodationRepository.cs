using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IAccommodationRepository : IRepository<Accommodation>
    {
        // Existing methods
        Task<IEnumerable<Accommodation>> GetAccommodationsByStudentAsync(Guid studentId);
        Task<IEnumerable<Accommodation>> GetActiveAccommodationsAsync();
        Task<Accommodation> GetAccommodationWithDetailsAsync(Guid accommodationId);
        Task<AccommodationAssignment> GetAssignmentByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<AccommodationAssignment> GetAssignmentWithDetailsAsync(Guid assignmentId, CancellationToken cancellationToken = default);
        Task UpdateAssignmentAsync(AccommodationAssignment assignment, CancellationToken cancellationToken = default);
        Task<AccommodationAssignment> AddAssignmentAsync(AccommodationAssignment assignment, CancellationToken cancellationToken = default);
        Task<AccommodationAssignment> GetAssignmentByStudentAndSemesterAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default);

        // ===== Lane Management =====
        Task<Lane> AddLaneAsync(Lane lane, CancellationToken cancellationToken = default);
        Task UpdateLaneAsync(Lane lane, CancellationToken cancellationToken = default);
        Task DeleteLaneAsync(Guid laneId, CancellationToken cancellationToken = default);
        Task<Lane?> GetLaneByIdAsync(Guid laneId, CancellationToken cancellationToken = default);
        Task<Lane?> GetLaneByNameAsync(string laneName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Lane>> GetLanesAsync(CancellationToken cancellationToken = default);
        Task<(IEnumerable<Lane> Items, int TotalCount)> GetLanesPagedAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default);
        Task<bool> LaneExistsAsync(string laneName, CancellationToken cancellationToken = default);
        Task<int> CountHousesInLaneAsync(Guid laneId, CancellationToken cancellationToken = default);
        Task<int> CountOccupiedHousesInLaneAsync(Guid laneId, CancellationToken cancellationToken = default);

        // ===== House Management =====
        Task<House> AddHouseAsync(House house, CancellationToken cancellationToken = default);
        Task<IEnumerable<House>> AddHousesRangeAsync(IEnumerable<House> houses, CancellationToken cancellationToken = default);
        Task UpdateHouseAsync(House house, CancellationToken cancellationToken = default);
        Task DeleteHouseAsync(Guid houseId, CancellationToken cancellationToken = default);
        Task<House?> GetHouseByIdAsync(Guid houseId, CancellationToken cancellationToken = default);
        Task<House?> GetHouseByNumberAsync(Guid laneId, string houseNumber, CancellationToken cancellationToken = default);
        Task<(IEnumerable<House> Items, int TotalCount)> GetHousesPagedAsync(int page, int pageSize, Guid? laneId, string? searchTerm, string? status, CancellationToken cancellationToken = default);
        Task<IEnumerable<House>> GetHousesByLaneAsync(Guid laneId, CancellationToken cancellationToken = default);
        Task<IEnumerable<House>> GetAvailableHousesAsync(Guid? laneId, CancellationToken cancellationToken = default);
        Task<bool> HouseExistsInLaneAsync(Guid laneId, string houseNumber, CancellationToken cancellationToken = default);
        Task<int> GetNextHouseNumberSequenceAsync(Guid laneId, CancellationToken cancellationToken = default);

        // ===== Occupancy & Reports =====
        Task<(int Total, int Occupied, int Vacant, int Maintenance, int Disabled, int Reserved)> GetLaneOccupancySummaryAsync(Guid laneId, CancellationToken cancellationToken = default);
        Task<(int Total, int Occupied, int Vacant, int Maintenance, int Disabled)> GetOverallOccupancySummaryAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<House>> GetHousesByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<IEnumerable<House>> GetHousesUnderMaintenanceAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AccommodationAssignment>> GetAssignmentsWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AccommodationAssignment>> GetAssignmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

        // ===== Legacy methods (kept for backward compatibility) =====
        Task<IEnumerable<Room>> GetRoomsAsync(int page, int pageSize, string? searchTerm, string? roomType, CancellationToken cancellationToken = default);
        Task<int> CountRoomsAsync(string? searchTerm, string? roomType, CancellationToken cancellationToken = default);
        Task<Room> GetRoomWithDetailsAsync(Guid roomId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Room>> GetAvailableRoomsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Room>> GetRoomsByBuildingAsync(string building, CancellationToken cancellationToken = default);
        Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken = default);
        Task<Building> AddBuildingAsync(Building building, CancellationToken cancellationToken = default);
        Task<Building?> GetBuildingByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<Building?> GetBuildingByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default);
    }
}
