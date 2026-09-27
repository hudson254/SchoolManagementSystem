using System;
using System.Collections.Generic;
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
    /// Regression tests for the production HTTP 500 on enrollment submission.
    ///
    /// <para>
    /// <b>Root cause.</b> <c>BaseEntity</c> pre-assigns <c>Id = Guid.NewGuid()</c>,
    /// so adding a new <c>Enrollment</c> through <c>student.Enrollments</c> made
    /// EF Core classify it <c>Modified</c> rather than <c>Added</c>. EF issued
    /// an UPDATE for a row that does not exist, affected 0 rows, and raised
    /// <c>DbUpdateConcurrencyException</c>, surfacing as HTTP 500.
    /// </para>
    ///
    /// <para>
    /// These tests drive the real endpoints over the real pipeline and then read
    /// the database directly, so a 2xx alone is never treated as proof.
    /// </para>
    /// </summary>
    public class EnrollmentSubmissionApiTests : IClassFixture<EnrollmentSubmissionFixture>
    {
        private readonly EnrollmentSubmissionFixture _fixture;

        public EnrollmentSubmissionApiTests(EnrollmentSubmissionFixture fixture)
        {
            _fixture = fixture;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(raw).RootElement.Clone();
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/v1/enrollment/submit-enrollment
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SubmitEnrollment_ReturnsSuccess_AndPersistsEnrollmentRows()
        {
            using var client = await _fixture.CreateNewStudentClientAsync();
            var studentId = await _fixture.GetStudentIdAsync();

            var response = await client.PostAsJsonAsync("/api/v1/enrollment/submit-enrollment",
                new { courseId = EnrollmentSubmissionFixture.CourseId });

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "a new Enrollment must be INSERTed, not UPDATEd; this used to return 500 " +
                "with DbUpdateConcurrencyException");

            var body = await ReadJsonAsync(response);
            body.GetProperty("studentId").GetGuid().Should().Be(studentId);
            body.GetProperty("courseId").GetGuid().Should().Be(EnrollmentSubmissionFixture.CourseId);
            body.GetProperty("unitsEnrolled").GetInt32().Should().Be(2,
                "one enrollment row per active unit of the selected course");
            body.GetProperty("status").GetString().Should().Be("PendingApproval");

            // Verify in the database, not just from the response.
            var rows = await _fixture.ReadEnrollmentsDirectlyAsync(studentId);
            rows.Should().HaveCount(2);
            rows.Select(r => r.UnitId).Should().BeEquivalentTo(new[]
            {
                EnrollmentSubmissionFixture.UnitOneId,
                EnrollmentSubmissionFixture.UnitTwoId
            });
            rows.Should().OnlyContain(r => r.Status == "PendingApproval");
            rows.Should().OnlyContain(r => r.IsActive == false);
            rows.Should().OnlyContain(r => r.TenantId == EnrollmentSubmissionFixture.DefaultTenantId);
        }

        [Fact]
        public async Task SubmitEnrollment_DoesNotReturnTheConcurrencyFailure()
        {
            using var client = await _fixture.CreateNewStudentClientAsync();

            var response = await client.PostAsJsonAsync("/api/v1/enrollment/submit-enrollment",
                new { courseId = EnrollmentSubmissionFixture.CourseId });

            response.IsSuccessStatusCode.Should().BeTrue();
            ((int)response.StatusCode).Should().BeInRange(200, 299);

            var raw = await response.Content.ReadAsStringAsync();
            raw.Should().NotContain("DbUpdateConcurrencyException");
            raw.Should().NotContain("expected to affect 1 row(s)");
        }

        [Fact]
        public async Task MyStatus_AfterSubmission_ReportsThePersistedSelection()
        {
            using var client = await _fixture.CreateNewStudentClientAsync();

            var submit = await client.PostAsJsonAsync("/api/v1/enrollment/submit-enrollment",
                new { courseId = EnrollmentSubmissionFixture.CourseId });
            submit.IsSuccessStatusCode.Should().BeTrue();

            var status = await client.GetAsync("/api/v1/enrollment/my-status");
            status.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadJsonAsync(status);
            body.GetProperty("registrationStatus").GetString().Should().Be("PendingApproval");
            body.GetProperty("selectedCourseId").GetGuid().Should().Be(EnrollmentSubmissionFixture.CourseId,
                "the course selection from 5dc711f9 must remain correct after submission");
            body.GetProperty("hasSelectedCourse").GetBoolean().Should().BeTrue();
            body.GetProperty("isPendingApproval").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task SubmitEnrollment_SurvivesLogoutAndLogin()
        {
            Guid studentId;

            using (var first = await _fixture.CreateNewStudentClientAsync())
            {
                studentId = await _fixture.GetStudentIdAsync();

                var submit = await first.PostAsJsonAsync("/api/v1/enrollment/submit-enrollment",
                    new { courseId = EnrollmentSubmissionFixture.CourseId });
                submit.IsSuccessStatusCode.Should().BeTrue();
            }

            // A brand new client and a brand new login for the SAME student.
            using var second = await _fixture.CreateReloginClientAsync();

            var status = await second.GetAsync("/api/v1/enrollment/my-status");
            status.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadJsonAsync(status);
            body.GetProperty("studentId").GetGuid().Should().Be(studentId);
            body.GetProperty("registrationStatus").GetString().Should().Be("PendingApproval");
            body.GetProperty("selectedCourseId").GetGuid()
                .Should().Be(EnrollmentSubmissionFixture.CourseId,
                    "the persisted course selection must survive logout and login");

            var rows = await _fixture.ReadEnrollmentsDirectlyAsync(studentId);
            rows.Should().HaveCount(2, "enrollment rows must still be there after logout/login");
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/v1/returning-user/enroll - the same defect existed here
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ReturningStudent_Enroll_ReturnsSuccess_AndPersistsEnrollmentRows()
        {
            using var client = await _fixture.CreateReturningStudentClientAsync();
            var studentId = await _fixture.GetStudentIdAsync();

            var response = await client.PostAsJsonAsync("/api/v1/returning-user/enroll",
                new
                {
                    courseId = EnrollmentSubmissionFixture.CourseId,
                    semesterId = EnrollmentSubmissionFixture.SemesterId
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "the returning-student path had the identical entity-state defect and must be repaired too");

            var body = await ReadJsonAsync(response);
            body.GetProperty("studentId").GetGuid().Should().Be(studentId);
            body.GetProperty("unitsEnrolled").GetInt32().Should().Be(2);
            body.GetProperty("status").GetString().Should().Be("Active");

            var raw = await response.Content.ReadAsStringAsync();
            raw.Should().NotContain("DbUpdateConcurrencyException");

            var rows = await _fixture.ReadEnrollmentsDirectlyAsync(studentId);
            rows.Should().HaveCount(2);
            rows.Select(r => r.UnitId).Should().BeEquivalentTo(new[]
            {
                EnrollmentSubmissionFixture.UnitOneId,
                EnrollmentSubmissionFixture.UnitTwoId
            });
            rows.Should().OnlyContain(r => r.Status == "Active");
            rows.Should().OnlyContain(r => r.IsActive);
            rows.Should().OnlyContain(r => r.TenantId == EnrollmentSubmissionFixture.DefaultTenantId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Authorization boundaries from the 5dc711f9 repair must be unchanged
        // ─────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("/api/v1/courses")]
        [InlineData("/api/v1/courses/99999999-9999-9999-9999-999999999999")]
        [InlineData("/api/v1/courses/99999999-9999-9999-9999-999999999999/units")]
        public async Task Student_IsStillForbiddenFromModeratorCourseEndpoints(string path)
        {
            using var client = await _fixture.CreateNewStudentClientAsync();

            var response = await client.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "the enrollment repair must not widen authorization");
        }

        [Fact]
        public async Task Student_StillCannotCreateOrDeleteCourses()
        {
            using var client = await _fixture.CreateNewStudentClientAsync();

            var create = await client.PostAsJsonAsync("/api/v1/courses",
                new { name = "Forbidden Course", code = "FORBID1" });
            create.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var delete = await client.DeleteAsync(
                "/api/v1/courses/99999999-9999-9999-9999-999999999999");
            delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Student_AuthorizedEnrollmentReadEndpoints_StillReturn200()
        {
            using var client = await _fixture.CreateNewStudentClientAsync();

            var courses = await client.GetAsync("/api/v1/enrollment/available-courses");
            courses.StatusCode.Should().Be(HttpStatusCode.OK);

            var units = await client.GetAsync(
                $"/api/v1/enrollment/available-courses/{EnrollmentSubmissionFixture.CourseId}/units");
            units.StatusCode.Should().Be(HttpStatusCode.OK);

            var unitList = await ReadJsonAsync(units);
            unitList.ValueKind.Should().Be(JsonValueKind.Array);
            unitList.EnumerateArray().Should().HaveCountGreaterThanOrEqualTo(2,
                "both seeded units must be offered to the student");
        }
    }
}
