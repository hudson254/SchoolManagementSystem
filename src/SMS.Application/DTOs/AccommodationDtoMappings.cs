using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Linq;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Shared mapping helpers for the Lane → House accommodation model so that
    /// every query exposes the same house/assignment shape (capacity, occupancy,
    /// mixed occupant types, check-in state).
    /// </summary>
    public static class AccommodationDtoMappings
    {
        public static HouseDto ToHouseDto(House h)
        {
            // Occupant display info: OccupantId/OccupantType are denormalized
            // primary-occupant markers. When the active assignments collection is
            // already loaded (batch queries), enrich the display names from it.
            // When not loaded, fall back to the denormalized markers (ids only).
            string? occupantName = null;
            string? studentNumber = null;
            string? employeeNumber = null;

            var activeAssignments = h.AccommodationAssignments
                .Where(a => a.Status == "Active")
                .ToList();
            var student = activeAssignments.Select(a => a.Student).FirstOrDefault(s => s != null);
            var lecturer = activeAssignments.Select(a => a.Lecturer).FirstOrDefault(l => l != null);

            if (h.OccupantType == OccupantType.Lecturer)
            {
                occupantName = lecturer != null ? $"{lecturer.FirstName} {lecturer.LastName}" : null;
                employeeNumber = lecturer?.EmployeeNumber;
            }
            else if (h.OccupantType == OccupantType.Student)
            {
                occupantName = student != null ? $"{student.FirstName} {student.LastName}" : null;
                studentNumber = student?.StudentNumber;
            }
            else
            {
                occupantName = student != null ? $"{student.FirstName} {student.LastName}"
                    : lecturer != null ? $"{lecturer.FirstName} {lecturer.LastName}"
                    : null;
                studentNumber = student?.StudentNumber;
                employeeNumber = lecturer?.EmployeeNumber;
            }

            return new HouseDto
            {
                Id = h.Id,
                LaneId = h.LaneId,
                LaneName = h.Lane?.LaneName ?? string.Empty,
                HouseNumber = h.HouseNumber,
                HouseName = h.HouseName,
                HouseNumberNumeric = h.HouseNumberNumeric,
                Status = h.Status,
                IsOccupied = h.IsOccupied || h.OccupiedCount > 0,
                IsEnabled = h.IsEnabled,
                IsAvailable = h.IsAvailable,
                Capacity = h.Capacity,
                OccupiedCount = h.OccupiedCount,
                OccupantId = h.OccupantId,
                OccupantType = h.OccupantType,
                OccupantName = occupantName,
                StudentNumber = studentNumber,
                EmployeeNumber = employeeNumber,
                SemesterId = h.SemesterId,
                Notes = h.Notes,
                OccupiedDate = h.OccupiedDate,
                CreatedDate = h.CreatedDate.GetValueOrDefault(),
                UpdatedDate = h.ModifiedDate
            };
        }

        public static string BuildDisplayName(Domain.Entities.Student? student)
        {
            if (student == null) return "Unknown";
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(student.Title)) parts.Add(student.Title);
            parts.Add(student.FirstName);
            if (!string.IsNullOrWhiteSpace(student.MiddleName)) parts.Add(student.MiddleName);
            parts.Add(student.LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }

        public static string BuildDisplayName(Domain.Entities.Lecturer? lecturer)
        {
            if (lecturer == null) return "Unknown";
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(lecturer.Title)) parts.Add(lecturer.Title);
            parts.Add(lecturer.FirstName);
            if (!string.IsNullOrWhiteSpace(lecturer.MiddleName)) parts.Add(lecturer.MiddleName);
            parts.Add(lecturer.LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }

        public static AccommodationAssignmentDto ToAssignmentDto(AccommodationAssignment a)
        {
            return new AccommodationAssignmentDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                LecturerId = a.LecturerId,
                OccupantType = a.OccupantType,
                RoomId = a.RoomId ?? Guid.Empty,
                SemesterId = a.SemesterId,
                AssignmentDate = a.AssignmentDate,
                MoveInDate = a.MoveInDate,
                MoveOutDate = a.MoveOutDate,
                CheckInDate = a.CheckInDate,
                CheckOutDate = a.CheckOutDate,
                Status = a.Status,
                Remarks = a.Remarks,
                StudentName = BuildDisplayName(a.Student),
                StudentNumber = a.Student?.StudentNumber ?? string.Empty,
                LecturerName = BuildDisplayName(a.Lecturer),
                EmployeeNumber = a.Lecturer?.EmployeeNumber ?? string.Empty,
                RoomNumber = a.Room?.RoomNumber ?? string.Empty,
                BlockName = a.Room?.Block?.Name ?? string.Empty,
                BuildingName = a.Room?.Block?.Building ?? string.Empty,
                SemesterName = a.Semester?.Name ?? string.Empty,
                HouseId = a.HouseId != Guid.Empty ? a.HouseId : null,
                HouseNumber = a.House?.HouseNumber ?? string.Empty,
                HouseName = a.House?.HouseName,
                LaneId = a.House?.LaneId ?? a.LaneId,
                LaneName = a.Lane?.LaneName ?? a.House?.Lane?.LaneName ?? string.Empty,
                HouseCapacity = a.House?.Capacity ?? 0,
                HouseOccupiedCount = a.House?.OccupiedCount ?? 0
            };
        }
    }
}