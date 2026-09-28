using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Regression tests for the two reported production defects:
    ///
    /// <list type="number">
    /// <item>A course chosen at registration is not reflected on the student
    /// details / dashboard (<c>GET /api/v1/enrollment/my-status</c> and
    /// <c>GET /api/v1/dashboard/student-me</c>).</item>
    /// <item>The course-selection page has no selectable course for a student
    /// (<c>/course-selection</c> step 1 and step 2).</item>
    /// </list>
    ///
    /// <para>
    /// Every request below is made with a real bearer token whose claims carry
    /// the "Student" role, so these tests exercise the real authorization
    /// pipeline rather than a mocked policy result.
    /// </para>
    /// </summary>
    public class StudentCourseSelectionApiTests : IClassFixture<StudentCourseSelectionFixture>
    {
        private readonly StudentCourseSelectionFixture _fixture;

        public StudentCourseSelectionApiTests(StudentCourseSelectionFixture fixture)
        {
            _fixture = fixture;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(raw).RootElement.Clone();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Symptom B: the student-authorized read path works for a Student
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Student_CanListAvailableCourses_InsteadOfGetting403()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync("/api/v1/enrollment/available-courses");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "the wizard needs a student-authorized course list; GET /courses is ModeratorAccess");

            var courses = await ReadJsonAsync(response);
            courses.ValueKind.Should().Be(JsonValueKind.Array);
            courses.EnumerateArray().Should().Contain(c =>
                c.GetProperty("id").GetString() == StudentCourseSelectionFixture.SelectableCourseId.ToString(),
                "the seeded selectable course must be returned to the student");
        }

        [Fact]
        public async Task Student_CanListUnitsOfTheChosenCourse_InsteadOfGetting403()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/enrollment/available-courses/{StudentCourseSelectionFixture.SelectableCourseId}/units");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "wizard step 2 needs a student-authorized unit list; GET /courses/{id}/units is ModeratorAccess");

            var units = await ReadJsonAsync(response);
            units.ValueKind.Should().Be(JsonValueKind.Array);
            units.EnumerateArray().Should().Contain(u =>
                u.GetProperty("id").GetString() == StudentCourseSelectionFixture.SelectableUnitId.ToString());
        }

        [Fact]
        public async Task Student_UnitsForAnUnknownCourse_ReturnsNotFound_Not500()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/enrollment/available-courses/{Guid.NewGuid()}/units");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Authorization boundary: NOT weakened
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Student_StillGetsForbiddenFromTheModeratorCourseList()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync("/api/v1/courses?isActive=true&pageSize=100");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "GET /api/v1/courses must stay behind ModeratorAccess");
        }

        [Fact]
        public async Task Student_StillGetsForbiddenFromASingleCourse()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/courses/{StudentCourseSelectionFixture.SelectableCourseId}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "GET /api/v1/courses/{id} must stay behind ModeratorAccess");
        }

        [Fact]
        public async Task Student_StillGetsForbiddenFromCourseUnits()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/courses/{StudentCourseSelectionFixture.SelectableCourseId}/units");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "GET /api/v1/courses/{id}/units must stay behind ModeratorAccess");
        }

        [Fact]
        public async Task Anonymous_CannotListAvailableCourses()
        {
            using var client = _fixture.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            var response = await client.GetAsync("/api/v1/enrollment/available-courses");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "the new endpoint is for an authenticated student feature, not an anonymous registration helper");
        }

        // ─────────────────────────────────────────────────────────────────────
        // The registration verification read path.
        //
        // A registrant has no account yet, so neither ModeratorAccess nor
        // StudentAccess can serve the review step. These tests lock in that the
        // anonymous endpoint exists, returns the same units the enrollment
        // command would persist, and does not leak another tenant's curriculum.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Anonymous_CanReadCourseUnitsForTheRegistrationReviewStep()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync(
                $"/api/v1/auth/active-courses/{StudentCourseSelectionFixture.SelectableCourseId}/units");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "the registration review step runs before login, so its unit list must be anonymously readable");

            var units = await ReadJsonAsync(response);
            units.ValueKind.Should().Be(JsonValueKind.Array);
            units.EnumerateArray().Should().Contain(u =>
                u.GetProperty("id").GetString() == StudentCourseSelectionFixture.SelectableUnitId.ToString());

            // The review page must be able to render the unit code and name.
            var unit = units.EnumerateArray().First(u =>
                u.GetProperty("id").GetString() == StudentCourseSelectionFixture.SelectableUnitId.ToString());
            unit.GetProperty("code").GetString().Should().NotBeNullOrWhiteSpace();
            unit.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Anonymous_RegistrationUnits_Return404ForAnUnknownCourse()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync(
                $"/api/v1/auth/active-courses/{Guid.NewGuid()}/units");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "an unknown or cross-tenant course must be a 404, never another tenant's curriculum");
        }

        [Fact]
        public async Task ModeratorOnlyEndpoints_RemainClosedToStudents()
        {
            // Regression guard: opening an anonymous read path for registration
            // must not weaken the existing authorization boundary.
            using var client = await _fixture.CreateStudentClientAsync();

            var courses = await client.GetAsync("/api/v1/courses");
            courses.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var units = await client.GetAsync(
                $"/api/v1/courses/{StudentCourseSelectionFixture.SelectableCourseId}/units");
            units.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ─────────────────────────────────────────────────────────────────────
        // What the verification page shows is what the database actually stores.
        //
        // These close the loop the stakeholder asked for: the unit list served to
        // the review step, the ids the client submits, and the rows written to
        // PostgreSQL must be the same set.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task LecturerRegistration_PersistsExactlyTheVerifiedUnits()
        {
            using var client = _fixture.CreateClient();

            // 1. What the verification page shows the lecturer.
            var verified = await ReadJsonAsync(await client.GetAsync(
                $"/api/v1/auth/active-courses/{StudentCourseSelectionFixture.SelectableCourseId}/units"));
            var allIds = verified.EnumerateArray().Select(u => u.GetProperty("id").GetString()!).ToList();
            allIds.Should().NotBeEmpty();

            // Choose a strict subset, which is what a lecturer picking individual
            // units does.
            var chosen = allIds.Take(1).ToList();

            var email = $"lect.verify.{Guid.NewGuid():N}@example.com";
            var body = new
            {
                firstName = "Verify",
                lastName = "Lecturer",
                email,
                password = "Test123!@#Xyz",
                confirmPassword = "Test123!@#Xyz",
                phoneNumber = "+254700000001",
                organization = "Verify Org",
                role = "Lecturer",
                specialization = "Verification",
                courseId = StudentCourseSelectionFixture.SelectableCourseId.ToString(),
                unitIds = chosen
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                "a lecturer registering with a valid course and one of its units must be accepted");

            // 2. What actually landed in the database.
            using var scope = _fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var lecturer = await db.Lecturers.SingleAsync(l => l.Email == email);

            var allocatedUnitIds = await db.UnitAllocations
                .Where(a => a.LecturerId == lecturer.Id)
                .Select(a => a.UnitId)
                .ToListAsync();

            // The API transports ids as strings and the database stores Guids,
            // so compare on the normalized string form.
            allocatedUnitIds.Select(id => id.ToString())
                .Should().BeEquivalentTo(chosen,
                    "the persisted teaching assignment must match exactly what the user confirmed on the review step");
        }

        [Fact]
        public async Task LecturerRegistration_RejectsAUnitFromAnotherCourse()
        {
            using var client = _fixture.CreateClient();

            var email = $"lect.foreign.{Guid.NewGuid():N}@example.com";
            var body = new
            {
                firstName = "Tamper",
                lastName = "Lecturer",
                email,
                password = "Test123!@#Xyz",
                confirmPassword = "Test123!@#Xyz",
                phoneNumber = "+254700000002",
                organization = "Verify Org",
                role = "Lecturer",
                specialization = "Verification",
                courseId = StudentCourseSelectionFixture.SelectableCourseId.ToString(),
                // A unit id that is not part of the selected course.
                unitIds = new[] { Guid.NewGuid().ToString() }
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "a tampered payload must not be able to attach a unit from another course or tenant");

            using var scope = _fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Lecturers.AnyAsync(l => l.Email == email)).Should().BeFalse(
                "a rejected lecturer registration must not leave a half-created lecturer behind");
        }

        [Fact]
        public async Task LecturerRegistration_WithoutUnits_IsRejected()
        {
            using var client = _fixture.CreateClient();

            var email = $"lect.nounits.{Guid.NewGuid():N}@example.com";
            var body = new
            {
                firstName = "No",
                lastName = "Units",
                email,
                password = "Test123!@#Xyz",
                confirmPassword = "Test123!@#Xyz",
                phoneNumber = "+254700000003",
                organization = "Verify Org",
                role = "Lecturer",
                specialization = "Verification",
                courseId = StudentCourseSelectionFixture.SelectableCourseId.ToString(),
                unitIds = Array.Empty<string>()
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "a lecturer must choose the units they will teach; this used to be silently discarded");
        }

        [Fact]
        public async Task StudentRegistration_PersistsTheCourseShownOnTheReviewStep()
        {
            using var client = _fixture.CreateClient();

            // The unit list the review page renders for this course.
            var verified = await ReadJsonAsync(await client.GetAsync(
                $"/api/v1/auth/active-courses/{StudentCourseSelectionFixture.SelectableCourseId}/units"));
            var shownUnitIds = verified.EnumerateArray()
                .Select(u => u.GetProperty("id").GetString()!).ToList();
            shownUnitIds.Should().NotBeEmpty();

            var email = $"stud.verify.{Guid.NewGuid():N}@example.com";
            var body = new
            {
                firstName = "Verify",
                lastName = "Student",
                email,
                password = "Test123!@#Xyz",
                confirmPassword = "Test123!@#Xyz",
                phoneNumber = "+254700000004",
                organization = "Verify Org",
                role = "Student",
                courseId = StudentCourseSelectionFixture.SelectableCourseId.ToString(),
                unitIds = shownUnitIds
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/register", body);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            using var scope = _fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var student = await db.Students.SingleAsync(s => s.Email == email);
            student.SelectedCourseId.Should().Be(StudentCourseSelectionFixture.SelectableCourseId,
                "the course the student verified must be the course stored on the student record");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Symptom A: the persisted selection is readable through the API
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task MyStatus_ReturnsTheCoursePersistedAtRegistration()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync("/api/v1/enrollment/my-status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var status = await ReadJsonAsync(response);

            status.GetProperty("hasSelectedCourse").GetBoolean().Should().BeTrue(
                "the course chosen at registration must be reported as selected");
            status.GetProperty("selectedCourseId").GetString()
                .Should().Be(StudentCourseSelectionFixture.SelectableCourseId.ToString());
            status.GetProperty("selectedCourseName").GetString()
                .Should().Be("Student Selectable Course");
            status.GetProperty("registrationStatus").GetString()
                .Should().Be("PendingCourseSelection");

            // Consistency: a persisted selection means the student must not be
            // told they still need to choose a course.
            status.GetProperty("needsCourseSelection").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task MyStatus_ReportsNoSelection_WhenTheStudentHasNone()
        {
            using var client = await _fixture.CreateStudentClientWithoutSelectionAsync();

            var response = await client.GetAsync("/api/v1/enrollment/my-status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var status = await ReadJsonAsync(response);

            status.GetProperty("hasSelectedCourse").GetBoolean().Should().BeFalse(
                "the endpoint must never report a selection that does not exist");
            status.GetProperty("selectedCourseId").ValueKind.Should().Be(JsonValueKind.Null);
            status.GetProperty("needsCourseSelection").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task StudentDashboard_ReturnsThePendingSelectedCourseCard()
        {
            using var client = await _fixture.CreateStudentClientAsync();

            var response = await client.GetAsync("/api/v1/dashboard/student-me");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dashboard = await ReadJsonAsync(response);

            var pending = dashboard.GetProperty("pendingCourse");
            pending.ValueKind.Should().NotBe(JsonValueKind.Null,
                "a student pending approval must still see the course they selected");
            pending.GetProperty("courseId").GetString()
                .Should().Be(StudentCourseSelectionFixture.SelectableCourseId.ToString());
            pending.GetProperty("courseName").GetString().Should().Be("Student Selectable Course");
            pending.GetProperty("courseCode").GetString().Should().Be("SSC101");
            pending.GetProperty("status").GetString().Should().Be("PendingSelection");
            pending.GetProperty("requiresSubmission").GetBoolean().Should().BeTrue();

            // This student has no course-offering enrollment yet, so the
            // offering-backed list stays empty rather than being faked.
            dashboard.GetProperty("enrollments").GetArrayLength().Should().Be(0);
        }
    }
}
