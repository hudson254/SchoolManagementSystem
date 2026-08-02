using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class UnitAllocationRepositoryTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly Mock<ILogger<UnitAllocationRepository>> _loggerMock;

        public UnitAllocationRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _loggerMock = new Mock<ILogger<UnitAllocationRepository>>();
        }

        private UnitAllocationRepository CreateRepository(ApplicationDbContext context)
        {
            return new UnitAllocationRepository(context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetByLecturerAsync_ShouldReturnAllocationsForLecturer()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var allocation = new UnitAllocation
            {
                LecturerId = lecturerId,
                UnitId = unitId,
                SemesterId = semesterId,
                Status = "Active",
                IsPrimary = true
            };
            await repository.AddAsync(allocation);

            var other = new UnitAllocation
            {
                LecturerId = Guid.NewGuid(),
                UnitId = unitId,
                SemesterId = semesterId,
                Status = "Active"
            };
            await repository.AddAsync(other);
            await context.SaveChangesAsync();

            // Act
            var results = await repository.GetByLecturerAsync(lecturerId);

            // Assert
            results.Should().ContainSingle(a => a.Id == allocation.Id);
            results.Should().NotContain(a => a.Id == other.Id);
        }

        [Fact]
        public async Task IsLecturerAllocatedAsync_ShouldReturnTrue_WhenActiveAllocationExists()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            await repository.AddAsync(new UnitAllocation
            {
                LecturerId = lecturerId,
                UnitId = unitId,
                SemesterId = semesterId,
                Status = "Active"
            });
            await context.SaveChangesAsync();

            // Act
            var isAllocated = await repository.IsLecturerAllocatedAsync(lecturerId, unitId, semesterId);

            // Assert
            isAllocated.Should().BeTrue();
        }

        [Fact]
        public async Task IsLecturerAllocatedAsync_ShouldReturnFalse_WhenOnlyInactiveAllocationExists()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            await repository.AddAsync(new UnitAllocation
            {
                LecturerId = lecturerId,
                UnitId = unitId,
                SemesterId = semesterId,
                Status = "Inactive"
            });
            await context.SaveChangesAsync();

            // Act
            var isAllocated = await repository.IsLecturerAllocatedAsync(lecturerId, unitId, semesterId);

            // Assert
            isAllocated.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllocationCountByLecturerAsync_ShouldCountOnlyActiveAllocations()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var lecturerId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            await repository.AddAsync(new UnitAllocation
            {
                LecturerId = lecturerId,
                UnitId = Guid.NewGuid(),
                SemesterId = semesterId,
                Status = "Active"
            });
            await repository.AddAsync(new UnitAllocation
            {
                LecturerId = lecturerId,
                UnitId = Guid.NewGuid(),
                SemesterId = semesterId,
                Status = "Active"
            });
            await repository.AddAsync(new UnitAllocation
            {
                LecturerId = lecturerId,
                UnitId = Guid.NewGuid(),
                SemesterId = semesterId,
                Status = "Inactive"
            });
            await context.SaveChangesAsync();

            // Act
            var count = await repository.GetAllocationCountByLecturerAsync(lecturerId);

            // Assert
            count.Should().Be(2);
        }
    }
}
