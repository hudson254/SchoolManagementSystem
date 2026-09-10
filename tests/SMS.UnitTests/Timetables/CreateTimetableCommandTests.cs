using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.Timetables.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Timetables
{
    /// <summary>
    /// Regression tests for the repaired timetable creation workflow.
    ///
    /// Root cause fixed: CreateTimetableCommand previously dropped UnitId /
    /// LecturerId / Date — they were never persisted, leaving the NOT NULL
    /// UnitId column unset which produced HTTP 500 on every insert. These tests
    /// lock in that all class-related fields survive into the entity.
    /// </summary>
    public class CreateTimetableCommandTests
    {
        private readonly Mock<ITimetableRepository> _timetableRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateTimetableCommandTests()
        {
            _timetableRepositoryMock = new Mock<ITimetableRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public async Task Handle_ShouldPersistUnitLecturerDateAndTimes()
        {
            var command = new CreateTimetableCommand
            {
                ClassId = Guid.NewGuid(),
                UnitId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                Date = new DateTime(2026, 9, 21),
                DayOfWeek = "Monday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                Venue = "Room A"
            };

            _timetableRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Timetable>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Timetable t, CancellationToken ct) => t);

            var handler = new CreateTimetableHandler(
                _timetableRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateTimetableHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.ClassId.Should().Be(command.ClassId);
            result.UnitId.Should().Be(command.UnitId);
            result.LecturerId.Should().Be(command.LecturerId);
            result.Date.Should().Be(command.Date);
            result.DayOfWeek.Should().Be("Monday");
            result.StartTime.Should().Be(command.StartTime);
            result.EndTime.Should().Be(command.EndTime);
            result.Venue.Should().Be("Room A");

            _timetableRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Timetable>(t =>
                        t.UnitId == command.UnitId &&
                        t.LecturerId == command.LecturerId &&
                        t.Date == command.Date &&
                        t.ClassId == command.ClassId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldPersistUnitLecturerDateAndTimes()
        {
            var entryId = Guid.NewGuid();
            var existing = new Timetable { Id = entryId };
            var command = new UpdateTimetableCommand
            {
                Id = entryId,
                ClassId = Guid.NewGuid(),
                UnitId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                Date = new DateTime(2026, 10, 1),
                DayOfWeek = "Tuesday",
                StartTime = new TimeSpan(11, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                Venue = "Lab B"
            };

            _timetableRepositoryMock
                .Setup(x => x.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var handler = new UpdateTimetableHandler(
                _timetableRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<UpdateTimetableHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.UnitId.Should().Be(command.UnitId);
            result.LecturerId.Should().Be(command.LecturerId);
            result.Date.Should().Be(command.Date);
            result.DayOfWeek.Should().Be("Tuesday");
            result.Venue.Should().Be("Lab B");

            existing.UnitId.Should().Be(command.UnitId);
            existing.LecturerId.Should().Be(command.LecturerId);
            existing.Date.Should().Be(command.Date);
            existing.DayOfWeek.Should().Be("Tuesday");
        }
    }
}