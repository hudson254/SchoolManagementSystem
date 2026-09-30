using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Enrollments.Commands;
using SMS.Application.Features.ReturningUser.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Real-PostgreSQL regression coverage for the production HTTP 500 on
    /// <c>POST /api/v1/enrollment/submit-enrollment</c>.
    ///
    /// <para>
    /// <b>Root cause.</b> <c>BaseEntity</c> pre-assigns <c>Id = Guid.NewGuid()</c>.
    /// Attaching a brand-new <see cref="Enrollment"/> through the
    /// <c>student.Enrollments</c> navigation collection made EF Core's change
    /// tracker discover the entity with a non-default key and classify it
    /// <c>Modified</c> instead of <c>Added</c>. EF then issued an UPDATE for a
    /// row that does not exist, affected 0 rows and raised
    /// <see cref="DbUpdateConcurrencyException"/>, which surfaced as HTTP 500.
    /// </para>
    ///
    /// <para>
    /// These tests run the REAL handlers against the REAL repositories on a
    /// REAL PostgreSQL database, and verify the persisted rows by querying the
    /// database directly rather than trusting the API response or the EF change
    /// tracker.
    /// </para>
    /// </summary>
    public class EnrollmentEntityStateTests : IClassFixture<EnrollmentEntityStateFixture>
    {
        private readonly EnrollmentEntityStateFixture _fixture;

        public EnrollmentEntityStateTests(EnrollmentEntityStateFixture fixture)
        {
            _fixture = fixture;
        }

        private sealed record SeededStudent(Student Student, Course Course, List<Unit> Units, string Email);

        private sealed record SeededOffering(Student Student, CourseOffering Offering, Guid SemesterId);

        // ─────────────────────────────────────────────────────────────────────
        // Seeding
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<SeededStudent> SeedStudentAsync(
            ApplicationDbContext context,
            RegistrationStatus status)
        {
            var tenant = EnrollmentEntityStateFixture.TenantId;
            var email = $"enroll-state-{Guid.NewGuid():N}@school.com";

            var course = new Course
            {
                Name = "Enrollment State Course",
                Code = $"ESC{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                Credits = 3,
                Duration = 1,
                IsActive = true,
                TenantId = tenant
            };

            var student = new Student
            {
                FirstName = "Enroll",
                LastName = "State",
                Email = email,
                StudentNumber = $"ENR-{Guid.NewGuid():N}"[..16],
                IsActive = true,
                IsEnrolled = false,
                RegistrationStatus = status,
                // The durable record of the chosen course, added in 5dc711f9.
                SelectedCourseId = course.Id,
                TenantId = tenant
            };

            var units = new List<Unit>
            {
                new Unit
                {
                    Code = $"U{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                    Name = "State Unit One",
                    Credits = 3,
                    CourseId = course.Id,
                    IsActive = true,
                    TenantId = tenant
                },
                new Unit
                {
                    Code = $"U{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                    Name = "State Unit Two",
                    Credits = 3,
                    CourseId = course.Id,
                    IsActive = true,
                    TenantId = tenant
                }
            };

            context.Courses.Add(course);
            context.Students.Add(student);
            context.Units.AddRange(units);
            await context.SaveChangesAsync();

            return new SeededStudent(student, course, units, email);
        }

        private static async Task<SeededOffering> SeedStudentWithOfferingAsync(
            ApplicationDbContext context,
            RegistrationStatus status)
        {
            var seeded = await SeedStudentAsync(context, status);
            var year = 2026;
            var tenant = EnrollmentEntityStateFixture.TenantId;

            var academicYear = new AcademicYear
            {
                Name = $"{year}/{year + 1}",
                StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(year + 1, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                IsCurrent = true,
                TenantId = tenant
            };
            await context.AcademicYears.AddAsync(academicYear);
            await context.SaveChangesAsync();

            var semester = new Semester
            {
                Name = "Semester 1",
                SemesterNumber = 1,
                StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(year, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                IsCurrent = true,
                AcademicYearId = academicYear.Id,
                TenantId = tenant
            };
            await context.Semesters.AddAsync(semester);
            await context.SaveChangesAsync();

            var offering = new CourseOffering
            {
                OfferingCode = $"{seeded.Course.Code}-{year}-S1-{Guid.NewGuid():N}"[..24],
                CourseId = seeded.Course.Id,
                AcademicYearId = academicYear.Id,
                SemesterId = semester.Id,
                Intake = $"{year} Intake A",
                StartDate = new DateTime(year, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(year, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                Status = CourseOfferingStatus.Active,
                IsActive = true,
                TenantId = tenant
            };
            await context.CourseOfferings.AddAsync(offering);
            await context.SaveChangesAsync();

            return new SeededOffering(seeded.Student, offering, semester.Id);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Real handler construction (real repositories, no EF mocking)
        // ─────────────────────────────────────────────────────────────────────

        private static Mock<SMS.Application.Common.Interfaces.ICurrentUserService> CurrentUser(string email)
        {
            var mock = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
            mock.Setup(x => x.UserId).Returns("integration-test-user");
            mock.Setup(x => x.Username).Returns("integration-test");
            mock.Setup(x => x.Email).Returns(email);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.Roles).Returns(new[] { "Student" });
            return mock;
        }

        private static SubmitStudentEnrollmentCommandHandler BuildSubmitHandler(
            ApplicationDbContext context, string email)
        {
            var loggers = NullLoggerFactory.Instance;

            return new SubmitStudentEnrollmentCommandHandler(
                CurrentUser(email).Object,
                new StudentRepository(context, loggers.CreateLogger<StudentRepository>()),
                new CourseRepository(context, loggers.CreateLogger<CourseRepository>()),
                new UnitRepository(context, loggers.CreateLogger<UnitRepository>()),
                new EnrollmentRepository(context, loggers.CreateLogger<EnrollmentRepository>()),
                new CourseOfferingRepository(context, loggers.CreateLogger<CourseOfferingRepository>()),
                new CourseOfferingEnrollmentRepository(context, loggers.CreateLogger<CourseOfferingEnrollmentRepository>()),
                new Mock<IAuditService>().Object,
                new UnitOfWork(context, loggers.CreateLogger<UnitOfWork>(), loggers),
                loggers.CreateLogger<SubmitStudentEnrollmentCommandHandler>());
        }

        private static SubmitReturningStudentEnrollmentCommandHandler BuildReturningHandler(
            ApplicationDbContext context, string email)
        {
            var loggers = NullLoggerFactory.Instance;

            return new SubmitReturningStudentEnrollmentCommandHandler(
                CurrentUser(email).Object,
                new StudentRepository(context, loggers.CreateLogger<StudentRepository>()),
                new CourseRepository(context, loggers.CreateLogger<CourseRepository>()),
                new UnitRepository(context, loggers.CreateLogger<UnitRepository>()),
                new EnrollmentRepository(context, loggers.CreateLogger<EnrollmentRepository>()),
                new Mock<IAuditService>().Object,
                new UnitOfWork(context, loggers.CreateLogger<UnitOfWork>(), loggers),
                loggers.CreateLogger<SubmitReturningStudentEnrollmentCommandHandler>());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Direct database verification (bypasses EF change tracker + query filter)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Publishes the tenant onto a raw ADO.NET connection so that the
        /// row level security policies evaluate against it.
        ///
        /// <para>A plain <see cref="NpgsqlConnection"/> is completely outside
        /// Entity Framework, so neither the command interceptor nor the
        /// connection interceptor runs for it. Without this the policies would
        /// evaluate against the all-zero sentinel and every direct read would
        /// return zero rows.</para>
        /// </summary>
        private static async Task PublishTenantAsync(NpgsqlConnection connection, Guid tenantId)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";
            command.Parameters.AddWithValue("tenant", tenantId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        private async Task<int> CountEnrollmentRowsDirectlyAsync(Guid studentId, Guid tenantId)
        {
            // Raw ADO.NET, deliberately bypassing EF. Row level security is
            // enforced by PostgreSQL on THIS connection, so the tenant context
            // has to be published explicitly here - a plain NpgsqlConnection
            // does not go through the runtime interceptors. Publishing it is
            // what makes this an honest end-to-end check: the count can only be
            // non-zero if the row really was written for this tenant AND the
            // policy admitted it.
            await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
            await connection.OpenAsync();
            await PublishTenantAsync(connection, tenantId);

            await using var command = connection.CreateCommand();
            command.CommandText =
                "select count(*) from \"Enrollments\" where \"StudentId\" = @student and \"tenant_id\" = @tenant";
            command.Parameters.AddWithValue("student", studentId);
            command.Parameters.AddWithValue("tenant", tenantId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        }

        private async Task<List<(Guid UnitId, string Status, bool IsActive, Guid TenantId, bool IsDeleted)>>
            ReadEnrollmentRowsDirectlyAsync(Guid studentId)
        {
            await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
            await connection.OpenAsync();
            await PublishTenantAsync(connection, EnrollmentEntityStateFixture.TenantId);

            await using var command = connection.CreateCommand();
            command.CommandText =
                "select \"UnitId\", \"Status\", \"IsActive\", \"tenant_id\", \"is_deleted\" " +
                "from \"Enrollments\" where \"StudentId\" = @student";
            command.Parameters.AddWithValue("student", studentId);

            var rows = new List<(Guid, string, bool, Guid, bool)>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add((reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2),
                    reader.GetGuid(3), reader.GetBoolean(4)));
            }

            return rows;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. The repository path: a new Enrollment is tracked as Added and INSERTs
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RepositoryAdd_TracksNewEnrollmentAsAdded_AndPersistsIt()
        {
            await using var context = _fixture.CreateContext();
            var seeded = await SeedStudentAsync(context, RegistrationStatus.PendingCourseSelection);
            var unit = seeded.Units[0];

            // BaseEntity pre-assigns the GUID: this is the very condition that made
            // the navigation-collection path misclassify the entity.
            var enrollment = new Enrollment
            {
                StudentId = seeded.Student.Id,
                CourseId = seeded.Course.Id,
                UnitId = unit.Id,
                EnrollmentDate = DateTime.UtcNow,
                Status = "PendingApproval",
                IsActive = false
            };
            enrollment.Id.Should().NotBe(Guid.Empty);

            var repository = new EnrollmentRepository(
                context, NullLoggerFactory.Instance.CreateLogger<EnrollmentRepository>());

            await repository.AddAsync(enrollment, CancellationToken.None);
            context.ChangeTracker.DetectChanges();

            context.Entry(enrollment).State.Should().Be(
                EntityState.Added,
                "the repository Add path must classify a new Enrollment as Added, not Modified");

            await context.SaveChangesAsync(CancellationToken.None);

            var count = await CountEnrollmentRowsDirectlyAsync(seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            count.Should().Be(1, "the INSERT must have reached PostgreSQL");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. Defect characterization: the navigation-collection path really is
        //    misclassified as Modified, and really does fail. This is the control
        //    that proves the tests below can detect the production regression.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task NavigationCollectionAdd_IsClassifiedAsModified_AndRaisesConcurrencyFailure()
        {
            await using var context = _fixture.CreateContext();
            var seeded = await SeedStudentAsync(context, RegistrationStatus.PendingCourseSelection);

            var student = await context.Students.SingleAsync(s => s.Id == seeded.Student.Id);
            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = seeded.Course.Id,
                UnitId = seeded.Units[0].Id,
                EnrollmentDate = DateTime.UtcNow,
                Status = "PendingApproval",
                IsActive = false
            };

            // The exact production pattern that caused the HTTP 500.
            student.Enrollments.Add(enrollment);
            context.ChangeTracker.DetectChanges();

            context.Entry(enrollment).State.Should().Be(
                EntityState.Modified,
                "a new Enrollment reached through the navigation collection is misclassified - this is the root cause");

            var save = async () => await context.SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<DbUpdateConcurrencyException>(
                "EF issues an UPDATE for a row that does not exist, affecting 0 rows");

            var count = await CountEnrollmentRowsDirectlyAsync(seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            count.Should().Be(0, "nothing is persisted by the navigation-collection path");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. SubmitStudentEnrollmentCommand end to end on real PostgreSQL
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitStudentEnrollment_InsertsEnrollments_WithoutConcurrencyFailure()
        {
            SeededStudent seeded;
            EnrollmentSubmissionResultDto result;

            await using (var context = _fixture.CreateContext())
            {
                seeded = await SeedStudentAsync(context, RegistrationStatus.PendingCourseSelection);
                var handler = BuildSubmitHandler(context, seeded.Email);

                EnrollmentSubmissionResultDto? captured = null;
                var act = async () =>
                {
                    captured = await handler.Handle(
                        new SubmitStudentEnrollmentCommand { CourseId = seeded.Course.Id },
                        CancellationToken.None);
                };

                // The whole point of the repair: no DbUpdateConcurrencyException,
                // therefore no HTTP 500.
                await act.Should().NotThrowAsync();
                result = captured!;
            }

            result.Status.Should().Be("PendingApproval");
            result.UnitsEnrolled.Should().Be(2);
            result.CourseId.Should().Be(seeded.Course.Id);
            result.CourseOfferingEnrollmentCreated.Should().BeFalse(
                "no active offering was seeded, so no offering must be fabricated");

            // Verify straight from PostgreSQL, not from the change tracker.
            var count = await CountEnrollmentRowsDirectlyAsync(seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            count.Should().Be(2, "one Enrollment row per active unit of the selected course");

            var rows = await ReadEnrollmentRowsDirectlyAsync(seeded.Student.Id);
            rows.Should().HaveCount(2);
            rows.Select(r => r.UnitId).Should().BeEquivalentTo(seeded.Units.Select(u => u.Id));
            rows.Should().OnlyContain(r => r.Status == "PendingApproval");
            rows.Should().OnlyContain(r => r.IsActive == false);
            rows.Should().OnlyContain(r => r.TenantId == EnrollmentEntityStateFixture.TenantId,
                "TenantId must be populated from the current tenant");
            rows.Should().OnlyContain(r => r.IsDeleted == false);

            // The durable course selection from 5dc711f9 must still be correct.
            await using (var verify = _fixture.CreateContext())
            {
                var student = await verify.Students.SingleAsync(s => s.Id == seeded.Student.Id);
                student.SelectedCourseId.Should().Be(seeded.Course.Id,
                    "the selected course must remain persisted on the student record");
                student.RegistrationStatus.Should().Be(RegistrationStatus.PendingApproval);
                student.IsEnrolled.Should().BeFalse();
            }
        }

        [Fact]
        public async Task SubmitStudentEnrollment_CreatesNoDuplicateEnrollmentRows()
        {
            SeededStudent seeded;

            await using (var context = _fixture.CreateContext())
            {
                seeded = await SeedStudentAsync(context, RegistrationStatus.PendingCourseSelection);
                var handler = BuildSubmitHandler(context, seeded.Email);

                await handler.Handle(
                    new SubmitStudentEnrollmentCommand { CourseId = seeded.Course.Id },
                    CancellationToken.None);
            }

            var count = await CountEnrollmentRowsDirectlyAsync(seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            count.Should().Be(2, "exactly one row per unit, with no duplicate enrollment rows");
        }

        [Fact]
        public async Task SubmitStudentEnrollment_PersistsPendingCourseOfferingEnrollment_WhenActiveOfferingExists()
        {
            SeededOffering seeded;
            EnrollmentSubmissionResultDto result;

            await using (var context = _fixture.CreateContext())
            {
                seeded = await SeedStudentWithOfferingAsync(context, RegistrationStatus.PendingCourseSelection);
                var handler = BuildSubmitHandler(context, seeded.Student.Email);

                EnrollmentSubmissionResultDto? captured = null;
                var act = async () =>
                {
                    captured = await handler.Handle(
                        new SubmitStudentEnrollmentCommand
                        {
                            CourseId = seeded.Student.SelectedCourseId!.Value,
                            SemesterId = seeded.SemesterId
                        },
                        CancellationToken.None);
                };

                await act.Should().NotThrowAsync();
                result = captured!;
            }

            result.CourseOfferingEnrollmentCreated.Should().BeTrue();
            result.CourseOfferingId.Should().Be(seeded.Offering.Id);

            await using (var verify = _fixture.CreateContext())
            {
                var offeringRows = await verify.CourseOfferingEnrollments
                    .Where(c => c.StudentId == seeded.Student.Id)
                    .ToListAsync();

                offeringRows.Should().ContainSingle();
                offeringRows[0].CourseOfferingId.Should().Be(seeded.Offering.Id);
                offeringRows[0].Status.Should().Be("PendingConfirmation",
                    "the offering-backed row stays pending and must not appear as an active enrollment");
            }

            var count = await CountEnrollmentRowsDirectlyAsync(seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            count.Should().Be(2);
        }

        private static async Task<Guid> SeedSemesterAsync(ApplicationDbContext context)
        {
            var tenant = EnrollmentEntityStateFixture.TenantId;
            var year = 2026;

            var academicYear = new AcademicYear
            {
                Name = $"{year}/{year + 1}",
                StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(year + 1, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                IsCurrent = true,
                TenantId = tenant
            };
            await context.AcademicYears.AddAsync(academicYear);
            await context.SaveChangesAsync();

            var semester = new Semester
            {
                Name = $"Semester {Guid.NewGuid():N}"[..14],
                SemesterNumber = 1,
                StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(year, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                IsCurrent = true,
                AcademicYearId = academicYear.Id,
                TenantId = tenant
            };
            await context.Semesters.AddAsync(semester);
            await context.SaveChangesAsync();

            return semester.Id;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. SubmitReturningStudentEnrollmentCommand - the same defect existed
        //    here, so the same repair must hold for returning students.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitReturningStudentEnrollment_InsertsEnrollments_WithoutConcurrencyFailure()
        {
            SeededStudent seeded;
            Guid semesterId;
            ReturningEnrollmentResultDto result;

            await using (var context = _fixture.CreateContext())
            {
                // A returning student is Approved, not PendingCourseSelection.
                seeded = await SeedStudentAsync(context, RegistrationStatus.Approved);
                semesterId = await SeedSemesterAsync(context);
                var handler = BuildReturningHandler(context, seeded.Email);

                ReturningEnrollmentResultDto? captured = null;
                var act = async () =>
                {
                    captured = await handler.Handle(
                        new SubmitReturningStudentEnrollmentCommand
                        {
                            CourseId = seeded.Course.Id,
                            SemesterId = semesterId
                        },
                        CancellationToken.None);
                };

                // The same misclassification used to break this path too.
                await act.Should().NotThrowAsync();
                result = captured!;
            }

            result.Status.Should().Be("Active");
            result.UnitsEnrolled.Should().Be(2);
            result.CourseId.Should().Be(seeded.Course.Id);

            var count = await CountEnrollmentRowsDirectlyAsync(seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            count.Should().Be(2, "the returning-student path must INSERT, not UPDATE a missing row");

            var rows = await ReadEnrollmentRowsDirectlyAsync(seeded.Student.Id);
            rows.Should().HaveCount(2);
            rows.Select(r => r.UnitId).Should().BeEquivalentTo(seeded.Units.Select(u => u.Id));
            rows.Should().OnlyContain(r => r.Status == "Active");
            rows.Should().OnlyContain(r => r.IsActive);
            rows.Should().OnlyContain(r => r.TenantId == EnrollmentEntityStateFixture.TenantId);

            await using (var verify = _fixture.CreateContext())
            {
                var student = await verify.Students.SingleAsync(s => s.Id == seeded.Student.Id);
                student.CurrentSemesterId.Should().Be(semesterId,
                    "the returning-student flow must still advance the current semester");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. Tenant isolation must be untouched by the repair
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task EnrollmentRows_WrittenForOneTenant_AreInvisibleToAnotherTenant()
        {
            SeededStudent seeded;

            await using (var context = _fixture.CreateContext())
            {
                seeded = await SeedStudentAsync(context, RegistrationStatus.PendingCourseSelection);
                var handler = BuildSubmitHandler(context, seeded.Email);

                await handler.Handle(
                    new SubmitStudentEnrollmentCommand { CourseId = seeded.Course.Id },
                    CancellationToken.None);
            }

            // The rows exist, scoped to the acting tenant.
            var directCount = await CountEnrollmentRowsDirectlyAsync(
                seeded.Student.Id, EnrollmentEntityStateFixture.TenantId);
            directCount.Should().Be(2);

            // Another tenant reading through the global query filter sees nothing.
            await using (var otherTenant = _fixture.CreateContext(
                "other-tenant@school.com", EnrollmentEntityStateFixture.OtherTenantId))
            {
                var visible = await otherTenant.Enrollments
                    .Where(e => e.StudentId == seeded.Student.Id)
                    .ToListAsync();

                visible.Should().BeEmpty(
                    "enrollment data must remain isolated per tenant");
            }

            // And no row may be stamped with a tenant other than the acting one.
            var rows = await ReadEnrollmentRowsDirectlyAsync(seeded.Student.Id);
            rows.Should().OnlyContain(r => r.TenantId == EnrollmentEntityStateFixture.TenantId);
        }

    }
}

