using SMS.Domain.Enums;

namespace SMS.Application.DTOs
{
    public class BuildingDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int TotalFloors { get; set; }
        public bool HasElevator { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public int TotalBlocks { get; set; }
        public int TotalRooms { get; set; }
    }

    public class BuildingDetailsDto : BuildingDto
    {
        public List<BlockDto> Blocks { get; set; } = new List<BlockDto>();
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public decimal OccupancyRate { get; set; }
    }

    public class BlockDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid BuildingId { get; set; }
        public int FloorNumber { get; set; }
        public int TotalRooms { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
    }

    public class RoomDto
    {
        public Guid Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public Guid BlockId { get; set; }
        public int Capacity { get; set; }
        public string? RoomType { get; set; }
        public decimal PricePerSemester { get; set; }
        public string? Facilities { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsOccupied { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BlockName { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string? CurrentOccupant { get; set; }
    }

    public class AccommodationAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? LecturerId { get; set; }
        public OccupantType OccupantType { get; set; } = OccupantType.Student;
        public Guid RoomId { get; set; }
        public Guid SemesterId { get; set; }
        public DateTime AssignmentDate { get; set; }
        public DateTime? MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AssignedBy { get; set; }
        public string? Remarks { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string BlockName { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
    }

    public class OccupancyReportDto
    {
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public decimal OccupancyRate { get; set; }
        public List<BuildingOccupancyDto> BuildingOccupancy { get; set; } = new List<BuildingOccupancyDto>();
    }

    public class BuildingOccupancyDto
    {
        public string BuildingName { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public decimal OccupancyRate { get; set; }
    }
}
