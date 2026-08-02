using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    // Unit Allocations DTO (unique)
    public class UnitAllocationDto { public Guid LecturerId { get; set; } public string LecturerName { get; set; } public Guid UnitId { get; set; } public string UnitCode { get; set; } public string UnitName { get; set; } public int CreditHours { get; set; } public string SemesterName { get; set; } public Guid? SemesterId { get; set; } }

    // Notification DTOs (unique)
    public class NotificationDto { public Guid Id { get; set; } public string Title { get; set; } public string Message { get; set; } public string Type { get; set; } public bool IsRead { get; set; } public DateTime CreatedAt { get; set; } public Guid? SenderId { get; set; } public string SenderName { get; set; } }
    public class UnreadCountDto { public int Count { get; set; } }

    // Report DTOs (unique)
    public class AssignmentCompletionReportDto { public int TotalAssignments { get; set; } public int Submitted { get; set; } public int Graded { get; set; } public double AverageScore { get; set; } public double CompletionRate { get; set; } }
    public class GradeDistributionReportDto { public int TotalStudents { get; set; } public Dictionary<string, int> GradeDistribution { get; set; } public double AverageGPA { get; set; } public double PassRate { get; set; } }
    public class UserActivityReportDto { public int TotalUsers { get; set; } public int ActiveUsers { get; set; } public int NewRegistrations { get; set; } public Dictionary<string, int> LoginsByDay { get; set; } }
    public class TimetableUtilizationReportDto { public int TotalSlots { get; set; } public int UsedSlots { get; set; } public double UtilizationRate { get; set; } public Dictionary<string, int> UsageByRoom { get; set; } }
    public class VacantRoomsReportDto { public int TotalVacant { get; set; } public IEnumerable<RoomDto> VacantRooms { get; set; } }
    // OccupancyReportDto and BuildingOccupancyDto are defined in AccommodationDto.cs

    // Conflict check (unique)
    public class ConflictCheckResultDto { public bool HasConflicts { get; set; } public IEnumerable<string> Conflicts { get; set; } }

    // Login history (unique)
    public class LoginHistoryDto { public Guid Id { get; set; } public string UserId { get; set; } public string IpAddress { get; set; } public string UserAgent { get; set; } public DateTime LoginTime { get; set; } public bool IsSuccessful { get; set; } }
}

