using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Accommodation.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    /// <summary>
    /// Contract-level tests for the two accommodation report endpoints the
    /// frontend accommodation service calls:
    ///   GET /api/v1/accommodation/reports/lane-occupancy/{laneId}
    ///   GET /api/v1/accommodation/reports/lecturer-accommodation
    ///
    /// Both callers previously disagreed with the backend. The lane report was
    /// requested without its required lane identifier, which cannot bind a route
    /// parameter, and was typed as a list although the handler returns a single
    /// report. The lecturer report sent a status filter the handler has never
    /// supported, while the laneId filter the controller does bind was ignored by
    /// the handler and so silently returned every lane. These tests pin the
    /// behaviour the routes promise so the two sides cannot drift apart again.
    /// </summary>
    public class LaneAndLecturerReportTests
    {
        private static readonly Guid LaneId = Guid.Parse("9c0e5c4a-1d2f-4a5b-8c7d-6e5f4a3b2c1d");
        private static readonly Guid OtherLaneId = Guid.Parse("1a1a1a1a-2b2b-3c3c-4d4d-5e5e5e5e5e5e");

        private readonly Mock<IAccommodationRepository> _repository = new();

        private GetLaneOccupancyReportHandler CreateLaneHandler() =>
            new(_repository.Object, new Mock<ILogger<GetLaneOccupancyReportHandler>>().Object);

        private GetLecturerAccommodationListHandler CreateLecturerHandler() =>
            new(_repository.Object, new Mock<ILogger<GetLecturerAccommodationListHandler>>().Object);

        private void SetupLane(Lane? lane) =>
            _repository
                .Setup(r => r.GetLaneByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lane);

        private void SetupHouses(Guid laneId, params House[] houses)
        {
            _repository
                .Setup(r => r.GetHousesByLaneAsync(laneId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(houses.ToList());
            _repository
                .Setup(r => r.GetLaneOccupancySummaryAsync(laneId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((
                    Total: houses.Length,
                    Occupied: houses.Count(h => h.Status == HouseStatus.Occupied),
                    Vacant: houses.Count(h => h.Status == HouseStatus.Vacant),
                    Maintenance: houses.Count(h => h.Status == HouseStatus.Maintenance),
                    Disabled: houses.Count(h => h.Status == HouseStatus.Disabled),
                    Reserved: houses.Count(h => h.Status == HouseStatus.Reserved)));
        }

        private static House NewHouse(Guid laneId, string number, int numeric, string status,
            int capacity, int occupied) =>
            new()
            {
                Id = Guid.NewGuid(),
                LaneId = laneId,
                Lane = new Lane { Id = laneId },
                HouseNumber = number,
                HouseNumberNumeric = numeric,
                Status = status,
                Capacity = capacity,
                OccupiedCount = occupied,
                IsEnabled = true,
            };

        // ---- reports/lane-occupancy/{laneId} ----

        [Fact]
        public async Task LaneOccupancy_WithLaneId_ReturnsTheReportForThatLane()
        {
            SetupLane(new Lane { Id = LaneId, LaneName = "Lane A", IsActive = true });
            SetupHouses(LaneId,
                NewHouse(LaneId, "001", 1, HouseStatus.Occupied, capacity: 2, occupied: 2),
                NewHouse(LaneId, "002", 2, HouseStatus.Vacant, capacity: 2, occupied: 0));

            var report = await CreateLaneHandler().Handle(
                new GetLaneOccupancyReportQuery { LaneId = LaneId }, CancellationToken.None);

            report.LaneId.Should().Be(LaneId);
            report.LaneName.Should().Be("Lane A");
            report.TotalHouses.Should().Be(2);
            report.Occupied.Should().Be(1);
            report.Vacant.Should().Be(1);
            report.TotalCapacity.Should().Be(4);
            report.Occupants.Should().Be(2);
            report.OccupancyPercentage.Should().Be(50);
            report.Houses.Should().HaveCount(2);
        }

        [Fact]
        public async Task LaneOccupancy_OnlyReadsTheRequestedLane()
        {
            SetupLane(new Lane { Id = LaneId, LaneName = "Lane A", IsActive = true });
            SetupHouses(LaneId);
            SetupHouses(OtherLaneId, NewHouse(OtherLaneId, "010", 10, HouseStatus.Occupied, 1, 1));

            await CreateLaneHandler().Handle(
                new GetLaneOccupancyReportQuery { LaneId = LaneId }, CancellationToken.None);

            _repository.Verify(r => r.GetHousesByLaneAsync(LaneId, It.IsAny<CancellationToken>()), Times.Once);
            _repository.Verify(r => r.GetHousesByLaneAsync(OtherLaneId, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LaneOccupancy_WhenLaneDoesNotExist_ThrowsNotFound()
        {
            // The same path covers another tenant's lane: the repository's tenant
            // filter makes it invisible, so it must surface as not-found rather
            // than an empty report that would leak the identifier's existence.
            SetupLane(null);

            var act = () => CreateLaneHandler().Handle(
                new GetLaneOccupancyReportQuery { LaneId = Guid.NewGuid() }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task LaneOccupancy_WithEmptyGuid_ThrowsNotFound()
        {
            // All-zeros is a well-formed Guid so it binds, but it is never a real
            // lane and must not produce a report.
            SetupLane(null);

            var act = () => CreateLaneHandler().Handle(
                new GetLaneOccupancyReportQuery { LaneId = Guid.Empty }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task LaneOccupancy_WhenLaneHasNoHouses_ReturnsZeroedReport()
        {
            SetupLane(new Lane { Id = LaneId, LaneName = "Empty Lane", IsActive = true });
            SetupHouses(LaneId);

            var report = await CreateLaneHandler().Handle(
                new GetLaneOccupancyReportQuery { LaneId = LaneId }, CancellationToken.None);

            report.TotalHouses.Should().Be(0);
            report.TotalCapacity.Should().Be(0);
            report.Occupants.Should().Be(0);
            // Guards against a divide-by-zero over an empty lane.
            report.OccupancyPercentage.Should().Be(0);
            report.Houses.Should().BeEmpty();
        }

        [Fact]
        public async Task LaneOccupancy_CountsUnavailableHousesSeparatelyFromVacantOnes()
        {
            SetupLane(new Lane { Id = LaneId, LaneName = "Lane B", IsActive = true });
            SetupHouses(LaneId,
                NewHouse(LaneId, "001", 1, HouseStatus.Occupied, capacity: 1, occupied: 1),
                NewHouse(LaneId, "002", 2, HouseStatus.Vacant, capacity: 1, occupied: 0),
                NewHouse(LaneId, "003", 3, HouseStatus.Unavailable, capacity: 1, occupied: 0),
                NewHouse(LaneId, "004", 4, HouseStatus.Maintenance, capacity: 1, occupied: 0));

            var report = await CreateLaneHandler().Handle(
                new GetLaneOccupancyReportQuery { LaneId = LaneId }, CancellationToken.None);

            report.TotalHouses.Should().Be(4);
            report.Occupied.Should().Be(1);
            report.Vacant.Should().Be(1);
            report.Unavailable.Should().Be(1);
            report.Maintenance.Should().Be(1);
            // Percentage is occupants over total capacity, not over house count.
            report.OccupancyPercentage.Should().Be(25);
        }

        // ---- reports/lecturer-accommodation ----

        private void SetupAssignments(params AccommodationAssignment[] assignments) =>
            _repository
                .Setup(r => r.GetAssignmentsWithDetailsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignments.ToList());

        private static AccommodationAssignment LecturerAssignment(
            Guid laneId, string houseNumber, int numeric, string employeeNumber,
            string firstName, string lastName, string status = "Active") =>
            new()
            {
                Id = Guid.NewGuid(),
                LaneId = laneId,
                HouseId = Guid.NewGuid(),
                House = new House
                {
                    LaneId = laneId,
                    HouseNumber = houseNumber,
                    HouseNumberNumeric = numeric,
                },
                Lane = new Lane { Id = laneId, LaneName = $"Lane {houseNumber}" },
                OccupantType = OccupantType.Lecturer,
                LecturerId = Guid.NewGuid(),
                Lecturer = new Lecturer
                {
                    EmployeeNumber = employeeNumber,
                    FirstName = firstName,
                    LastName = lastName,
                },
                Status = status,
                AssignedDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            };

        [Fact]
        public async Task LecturerAccommodation_WithoutFilters_ReturnsLecturerAssignmentsOnly()
        {
            SetupAssignments(
                LecturerAssignment(LaneId, "001", 1, "EMP-001", "Jane", "OKELLO"),
                new AccommodationAssignment
                {
                    Id = Guid.NewGuid(),
                    LaneId = LaneId,
                    HouseId = Guid.NewGuid(),
                    OccupantType = OccupantType.Student,
                    StudentId = Guid.NewGuid(),
                    Status = "Active",
                });

            var rows = (await CreateLecturerHandler().Handle(
                new GetLecturerAccommodationListQuery(), CancellationToken.None)).ToList();

            rows.Should().ContainSingle();
            rows[0].EmployeeNumber.Should().Be("EMP-001");
            rows[0].LecturerName.Should().Be("Jane OKELLO");
            rows[0].HouseNumber.Should().Be("001");
            rows[0].LaneName.Should().Be("Lane 001");
        }

        [Fact]
        public async Task LecturerAccommodation_LaneIdFilter_NarrowsToThatLane()
        {
            // The controller has always bound laneId; the handler used to drop it,
            // so a caller filtering by lane silently received every lane.
            SetupAssignments(
                LecturerAssignment(LaneId, "001", 1, "EMP-001", "Jane", "OKELLO"),
                LecturerAssignment(OtherLaneId, "010", 10, "EMP-002", "Peter", "KAMAU"));

            var rows = (await CreateLecturerHandler().Handle(
                new GetLecturerAccommodationListQuery { LaneId = LaneId }, CancellationToken.None)).ToList();

            rows.Should().ContainSingle().Which.EmployeeNumber.Should().Be("EMP-001");
        }

        [Fact]
        public async Task LecturerAccommodation_WithoutLaneIdFilter_ReturnsEveryLane()
        {
            SetupAssignments(
                LecturerAssignment(LaneId, "001", 1, "EMP-001", "Jane", "OKELLO"),
                LecturerAssignment(OtherLaneId, "010", 10, "EMP-002", "Peter", "KAMAU"));

            var rows = (await CreateLecturerHandler().Handle(
                new GetLecturerAccommodationListQuery(), CancellationToken.None)).ToList();

            rows.Should().HaveCount(2);
        }

        [Theory]
        [InlineData("OKELLO")]
        [InlineData("okello")]
        [InlineData("EMP-001")]
        [InlineData("Jane")]
        public async Task LecturerAccommodation_SearchTerm_MatchesNameAndEmployeeNumber(string term)
        {
            SetupAssignments(
                LecturerAssignment(LaneId, "001", 1, "EMP-001", "Jane", "OKELLO"),
                LecturerAssignment(LaneId, "002", 2, "EMP-002", "Peter", "KAMAU"));

            var rows = (await CreateLecturerHandler().Handle(
                new GetLecturerAccommodationListQuery { SearchTerm = term }, CancellationToken.None)).ToList();

            rows.Should().ContainSingle().Which.EmployeeNumber.Should().Be("EMP-001");
        }

        [Fact]
        public async Task LecturerAccommodation_WhenNothingMatches_ReturnsEmptyList()
        {
            SetupAssignments(LecturerAssignment(LaneId, "001", 1, "EMP-001", "Jane", "OKELLO"));

            var rows = (await CreateLecturerHandler().Handle(
                new GetLecturerAccommodationListQuery { SearchTerm = "NOBODY" }, CancellationToken.None)).ToList();

            rows.Should().BeEmpty();
        }

        [Fact]
        public async Task LecturerAccommodation_WhenAssignmentHasNoNavigationData_ReportsPlaceholders()
        {
            // A dangling navigation reference must not null-reference the report:
            // the DTO falls back to sentinels instead of dropping the row.
            SetupAssignments(new AccommodationAssignment
            {
                Id = Guid.NewGuid(),
                LaneId = LaneId,
                HouseId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                OccupantType = OccupantType.Lecturer,
                Status = "Active",
            });

            var rows = (await CreateLecturerHandler().Handle(
                new GetLecturerAccommodationListQuery { LaneId = LaneId }, CancellationToken.None)).ToList();

            rows.Should().ContainSingle();
            rows[0].LecturerName.Should().Be("Unknown");
            rows[0].EmployeeNumber.Should().Be("N/A");
            rows[0].HouseNumber.Should().BeNull();
        }

        // ---- reports/student-accommodation (the twin endpoint) ----

        private static AccommodationAssignment StudentAssignment(Guid laneId, string studentNumber, string lastName) =>
            new()
            {
                Id = Guid.NewGuid(),
                LaneId = laneId,
                HouseId = Guid.NewGuid(),
                Lane = new Lane { Id = laneId, LaneName = "Lane 001" },
                OccupantType = OccupantType.Student,
                StudentId = Guid.NewGuid(),
                Student = new Student { StudentNumber = studentNumber, FirstName = "Mary", LastName = lastName },
                Status = "Active",
            };

        [Fact]
        public async Task StudentAccommodation_LaneIdFilter_NarrowsToThatLane()
        {
            SetupAssignments(
                StudentAssignment(LaneId, "S/001", "WANJIRU"),
                StudentAssignment(OtherLaneId, "S/002", "OTieno"));

            var handler = new GetStudentAccommodationListHandler(
                _repository.Object, new Mock<ILogger<GetStudentAccommodationListHandler>>().Object);

            var rows = (await handler.Handle(
                new GetStudentAccommodationListQuery { LaneId = LaneId }, CancellationToken.None)).ToList();

            rows.Should().ContainSingle().Which.StudentNumber.Should().Be("S/001");
        }

        [Fact]
        public async Task StudentAccommodation_WithoutFilters_ReturnsStudentAssignmentsOnly()
        {
            SetupAssignments(
                StudentAssignment(LaneId, "S/001", "WANJIRU"),
                LecturerAssignment(LaneId, "002", 2, "EMP-002", "Peter", "KAMAU"));

            var handler = new GetStudentAccommodationListHandler(
                _repository.Object, new Mock<ILogger<GetStudentAccommodationListHandler>>().Object);

            var rows = (await handler.Handle(
                new GetStudentAccommodationListQuery(), CancellationToken.None)).ToList();

            rows.Should().ContainSingle().Which.StudentName.Should().Be("Mary WANJIRU");
        }
    }
}
