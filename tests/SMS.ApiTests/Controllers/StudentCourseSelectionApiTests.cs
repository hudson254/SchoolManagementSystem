using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
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
