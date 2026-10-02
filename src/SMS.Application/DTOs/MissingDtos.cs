using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    // Unit Allocations DTO (unique)
    public class UnitAllocationDto { public Guid LecturerId { get; set; } public string LecturerName { get; set; } public Guid UnitId { get; set; } public string UnitCode { get; set; } public string UnitName { get; set; } public int CreditHours { get; set; } public string SemesterName { get; set; } public Guid? SemesterId { get; set; } }

    // Notification DTOs (unique)
    /// <summary>
    /// A notification as returned by the API.
    /// <para>
    /// Field names are camel-cased by the API's JSON serializer. Note that
    /// <see cref="CreatedAt"/> serialises as <c>createdAt</c> and
    /// <see cref="CreatedDate"/> as <c>createdDate</c>: they are two aliases of the
    /// same instant, published so the existing frontend contract
    /// (<c>createdDate</c>) keeps working while new clients use the clearer
    /// <c>createdAt</c>. Both are always populated.
    /// </para>
    /// </summary>
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>One of <c>NotificationTypes</c>.</summary>
        public string Type { get; set; } = "System";

        public bool IsRead { get; set; }

        /// <summary>When the notification was created (UTC).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Alias of <see cref="CreatedAt"/> retained for the existing client contract.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>When the notification was read (UTC), or null while unread.</summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>Alias of <see cref="ReadAt"/> retained for the existing client contract.</summary>
        public DateTime? ReadDate { get; set; }

        /// <summary>One of <c>NotificationPriorities</c>.</summary>
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Application-relative navigation target, or null. Always validated to be a
        /// root-relative path on write; it is a hint only and never an authorization
        /// mechanism.
        /// </summary>
        public string? ActionUrl { get; set; }

        /// <summary>Optional expiry. The notification stays in history after this.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>True when <see cref="ExpiresAt"/> is in the past. Computed server-side.</summary>
        public bool IsExpired { get; set; }

        /// <summary>Identifier of the object this notification refers to, if any.</summary>
        public string? ReferenceId { get; set; }

        public Guid? SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Unread badge payload. <see cref="Count"/> is the total unread count shown on the
    /// bell; <see cref="HasCritical"/> lets the UI emphasise the badge without the
    /// client having to re-derive severity.
    /// </summary>
    public class UnreadCountDto { public int Count { get; set; } public bool HasCritical { get; set; } }

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

