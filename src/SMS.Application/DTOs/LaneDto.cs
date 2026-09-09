using SMS.Domain.Enums;
using System;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Data transfer object for Lane.
    /// </summary>
    public class LaneDto
    {
        public Guid Id { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? NumberingFormat { get; set; }
        public int StartingHouseNumber { get; set; }
        public int TotalHouses { get; set; }
        public int OccupiedHouses { get; set; }
        public int VacantHouses { get; set; }
        public int MaintenanceCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    /// <summary>
    /// Data transfer object for House.
    /// </summary>
    public class HouseDto
    {
        public Guid Id { get; set; }
        public Guid LaneId { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string? HouseName { get; set; }
        public int HouseNumberNumeric { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsAvailable { get; set; }
        public int Capacity { get; set; } = 1;
        public int OccupiedCount { get; set; }
        public Guid? OccupantId { get; set; }
        public OccupantType? OccupantType { get; set; }
        public string? OccupantName { get; set; }
        public string? StudentNumber { get; set; }
        public string? EmployeeNumber { get; set; }
        public Guid? SemesterId { get; set; }
        public string? Notes { get; set; }
        public DateTime? OccupiedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        /// <summary>Number of free places left before capacity is reached.</summary>
        public int RemainingCapacity => Math.Max(0, Capacity - OccupiedCount);
    }

    /// <summary>
    /// Summary of occupancy for a lane.
    /// </summary>
    public class LaneOccupancyDto
    {
        public Guid LaneId { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public int TotalHouses { get; set; }
        public int Occupied { get; set; }
        public int Vacant { get; set; }
        public int Reserved { get; set; }
        public int Maintenance { get; set; }
        public int Disabled { get; set; }
        public double OccupancyPercentage { get; set; }

        /// <summary>Total bed/place capacity across all houses in the lane.</summary>
        public int TotalCapacity { get; set; }

        /// <summary>Total current occupants (students + lecturers) across the lane.</summary>
        public int Occupants { get; set; }

        /// <summary>Current student occupants.</summary>
        public int StudentOccupants { get; set; }

        /// <summary>Current lecturer occupants.</summary>
        public int LecturerOccupants { get; set; }

        /// <summary>Free places remaining across the lane.</summary>
        public int AvailableCapacity => Math.Max(0, TotalCapacity - Occupants);
    }

    /// <summary>
    /// Overall accommodation occupancy summary.
    /// </summary>
    public class AccommodationDashboardDto
    {
        public int TotalLanes { get; set; }
        public int TotalHouses { get; set; }
        public int OccupiedHouses { get; set; }
        public int VacantHouses { get; set; }
        public int MaintenanceCount { get; set; }
        public int DisabledCount { get; set; }

        /// <summary>Total place capacity across all houses.</summary>
        public int TotalCapacity { get; set; }

        /// <summary>Total current occupants (students + lecturers).</summary>
        public int TotalOccupants { get; set; }

        /// <summary>Current student occupants.</summary>
        public int StudentOccupants { get; set; }

        /// <summary>Current lecturer occupants.</summary>
        public int LecturerOccupants { get; set; }

        /// <summary>Free places remaining.</summary>
        public int AvailableCapacity => Math.Max(0, TotalCapacity - TotalOccupants);

        public double OccupancyPercentage { get; set; }
        public List<LaneOccupancyDto> LaneSummaries { get; set; } = new();
    }

    /// <summary>
    /// Command to create a new lane.
    /// </summary>
    public class CreateLaneCommandDto
    {
        public string LaneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int NumberOfHouses { get; set; } = 10;
        public string? NumberingFormat { get; set; }
        public int StartingHouseNumber { get; set; } = 1;
    }

    /// <summary>
    /// Command to update an existing lane.
    /// </summary>
    public class UpdateLaneCommandDto
    {
        public Guid Id { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Command to create houses in a lane.
    /// </summary>
    public class CreateHouseCommandDto
    {
        public Guid LaneId { get; set; }
        public int NumberOfHouses { get; set; } = 1;
        public string? NumberingFormat { get; set; }
        public int StartingHouseNumber { get; set; }
    }

    /// <summary>
    /// Command to assign a student to a house.
    /// </summary>
    public class AssignHouseCommandDto
    {
        public Guid HouseId { get; set; }
        public Guid StudentId { get; set; }
        public Guid SemesterId { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Command to reassign a student to a different house.
    /// </summary>
    public class ReassignHouseCommandDto
    {
        public Guid CurrentHouseId { get; set; }
        public Guid NewHouseId { get; set; }
        public Guid StudentId { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Command to vacate a house.
    /// </summary>
    public class VacateHouseCommandDto
    {
        public Guid HouseId { get; set; }
        public Guid StudentId { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Report DTO for lane occupancy.
    /// </summary>
    public class LaneOccupancyReportDto
    {
        public Guid LaneId { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public int TotalHouses { get; set; }
        public int Occupied { get; set; }
        public int Vacant { get; set; }
        public int Reserved { get; set; }
        public int Maintenance { get; set; }
        public int Disabled { get; set; }
        public int Unavailable { get; set; }
        public double OccupancyPercentage { get; set; }

        /// <summary>Total place capacity across the lane.</summary>
        public int TotalCapacity { get; set; }

        /// <summary>Total current occupants (students + lecturers).</summary>
        public int Occupants { get; set; }

        /// <summary>Free places remaining.</summary>
        public int AvailableCapacity => Math.Max(0, TotalCapacity - Occupants);

        public List<HouseDto> Houses { get; set; } = new();
    }

    /// <summary>
    /// Report DTO for house occupancy.
    /// </summary>
    public class HouseOccupancyReportDto
    {
        public Guid HouseId { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string? HouseName { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public int Capacity { get; set; } = 1;
        public int Occupants { get; set; }
        public string? OccupantName { get; set; }
        public string? StudentNumber { get; set; }
        public string? EmployeeNumber { get; set; }
        public DateTime? OccupiedDate { get; set; }
        public DateTime? VacatedDate { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Report DTO for student accommodation list.
    /// </summary>
    public class StudentAccommodationDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public Guid? HouseId { get; set; }
        public string? HouseNumber { get; set; }
        public string? LaneName { get; set; }
        public string? AssignmentStatus { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime? MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Report DTO for lecturer accommodation list.
    /// </summary>
    public class LecturerAccommodationDto
    {
        public Guid LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public Guid? HouseId { get; set; }
        public string? HouseNumber { get; set; }
        public string? LaneName { get; set; }
        public string? AssignmentStatus { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime? MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Generic report DTO for any occupant accommodation list.
    /// </summary>
    public class OccupantAccommodationDto
    {
        public Guid OccupantId { get; set; }
        public OccupantType OccupantType { get; set; } = OccupantType.Student;
        public string OccupantName { get; set; } = string.Empty;
        public string? Identifier { get; set; }
        public Guid? HouseId { get; set; }
        public string? HouseNumber { get; set; }
        public string? LaneName { get; set; }
        public string? AssignmentStatus { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime? MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Report DTO for vacant houses.
    /// </summary>
    public class VacantHouseReportDto
    {
        public int TotalVacant { get; set; }
        public List<HouseDto> VacantHouses { get; set; } = new();
    }

    /// <summary>
    /// Report DTO for houses under maintenance.
    /// </summary>
    public class MaintenanceReportDto
    {
        public int TotalUnderMaintenance { get; set; }
        public List<HouseDto> HousesUnderMaintenance { get; set; } = new();
    }

    /// <summary>
    /// Report DTO for overall occupancy statistics.
    /// </summary>
    public class OccupancyStatisticsDto
    {
        public int TotalLanes { get; set; }
        public int TotalHouses { get; set; }
        public int OccupiedHouses { get; set; }
        public int VacantHouses { get; set; }
        public int ReservedHouses { get; set; }
        public int MaintenanceHouses { get; set; }
        public int DisabledHouses { get; set; }
        public int UnavailableHouses { get; set; }

        /// <summary>Total place capacity across all houses.</summary>
        public int TotalCapacity { get; set; }

        /// <summary>Total current occupants (students + lecturers).</summary>
        public int TotalOccupants { get; set; }

        /// <summary>Current student occupants.</summary>
        public int StudentOccupants { get; set; }

        /// <summary>Current lecturer occupants.</summary>
        public int LecturerOccupants { get; set; }

        /// <summary>Free places remaining.</summary>
        public int AvailableCapacity => Math.Max(0, TotalCapacity - TotalOccupants);

        /// <summary>House-status percentage (kept for backward compatibility).</summary>
        public double OccupancyPercentage { get; set; }

        /// <summary>Occupancy as a percentage of total place capacity.</summary>
        public double CapacityUtilization => TotalCapacity > 0 ? Math.Round((double)TotalOccupants / TotalCapacity * 100, 2) : 0;

        public List<LaneOccupancyDto> LaneSummaries { get; set; } = new();
    }
}
