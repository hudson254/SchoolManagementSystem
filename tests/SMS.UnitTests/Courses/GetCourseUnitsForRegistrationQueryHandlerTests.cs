using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Exceptions;
using SMS.Application.Features.Courses.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Courses
{
    /// <summary>
    /// Unit-level regression tests for the registration course/unit verification
    /// read path that feeds the public registration wizard's review step.
    ///
    /// Before the repair, no such endpoint existed: the wizard could only load
    /// courses (GET /auth/active-courses) and had no way to read a course's
    /// units before the registrant had an account, so the review step could
    /// never display them.
    /// </summary>
    public class GetCourseUnitsForRegistrationQueryHandlerTests
    {
        private readonly Mock<ICourseRepository> _courseRepository = new();
        private readonly Mock<IUnitRepository> _unitRepository = new();
        private readonly ILogger<GetCourseUnitsForRegistrationQueryHandler> _logger =
            Mock.Of<ILogger<GetCourseUnitsForRegistrationQueryHandler>>();

        private GetCourseUnitsForRegistrationQueryHandler CreateHandler() =>
            new(_courseRepository.Object, _unitRepository.Object, _logger);

        private static Course ActiveCourse(Guid id) => new()
        {
            Id = id,
            Code = "WLM",
            Name = "Diploma in Wildlife Management",
            IsActive = true
        };

        [Fact]
        public async Task Handle_ForActiveCourse_ReturnsActiveUnitsOrderedByCode()
        {
            var courseId = Guid.NewGuid();
            var u1 = new Unit { Id = Guid.NewGuid(), Code = "WLM102", Name = "Conservation Biology", CourseId = courseId, IsActive = true, Credits = 4 };
            var u2 = new Unit { Id = Guid.NewGuid(), Code = "WLM101", Name = "Wildlife Ecology", CourseId = courseId, IsActive = true, Credits = 4 };

            _courseRepository.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ActiveCourse(courseId));
            _unitRepository.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { u1, u2 });

            var result = await CreateHandler()
                .Handle(new GetCourseUnitsForRegistrationQuery { CourseId = courseId }, CancellationToken.None);

            result.Should().HaveCount(2);
            result.Select(u => u.Code).Should().ContainInOrder("WLM101", "WLM102");
        }

        [Fact]
        public async Task Handle_ExcludesInactiveAndDeletedUnits()
        {
            // These are exactly the units the enrollment command would persist,
            // so filtering them out here is what keeps the preview honest.
            var courseId = Guid.NewGuid();
            var active = new Unit { Id = Guid.NewGuid(), Code = "WLM101", Name = "Wildlife Ecology", CourseId = courseId, IsActive = true };
            var inactive = new Unit { Id = Guid.NewGuid(), Code = "WLM999", Name = "Retired Unit", CourseId = courseId, IsActive = false };
            var deleted = new Unit { Id = Guid.NewGuid(), Code = "WLM998", Name = "Deleted Unit", CourseId = courseId, IsActive = true, IsDeleted = true };

            _courseRepository.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ActiveCourse(courseId));
            _unitRepository.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { active, inactive, deleted });

            var result = await CreateHandler()
                .Handle(new GetCourseUnitsForRegistrationQuery { CourseId = courseId }, CancellationToken.None);

            result.Should().ContainSingle().Which.Code.Should().Be("WLM101");
        }

        [Fact]
        public async Task Handle_ForCourseFromAnotherTenant_ThrowsNotFound()
        {
            // The repository is tenant-scoped, so a foreign course id resolves
            // to null. It must be a 404, never another tenant's curriculum.
            var foreignCourseId = Guid.NewGuid();
            _courseRepository.Setup(x => x.GetByIdAsync(foreignCourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler()
                .Handle(new GetCourseUnitsForRegistrationQuery { CourseId = foreignCourseId }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ForInactiveCourse_ThrowsNotFound()
        {
            var courseId = Guid.NewGuid();
            _courseRepository.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Code = "OLD", Name = "Old Course", IsActive = false });

            await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler()
                .Handle(new GetCourseUnitsForRegistrationQuery { CourseId = courseId }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithEmptyCourseId_ThrowsValidation()
        {
            await Assert.ThrowsAsync<ValidationException>(() => CreateHandler()
                .Handle(new GetCourseUnitsForRegistrationQuery { CourseId = Guid.Empty }, CancellationToken.None));
        }
    }
}
