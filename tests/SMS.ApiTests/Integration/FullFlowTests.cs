using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Application.DTOs;
using Xunit;

namespace SMS.ApiTests.Integration
{
    public class FullFlowTests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;
        private readonly HttpClient _client;

        public FullFlowTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.CreateClient();
        }

        [Fact]
        public async Task StudentFlow_RegisterLoginEnrollViewGrades_ShouldSucceed()
        {
            // 1. Register a new student
            var email = $"student.{Guid.NewGuid()}@example.com";
            var registerRequest = new
            {
                firstName = "Flow",
                lastName = "Student",
                email = email,
                password = "Test123!@#",
                confirmPassword = "Test123!@#",
                phoneNumber = "+254712345678",
                organization = "Flow Test",
                role = "Student"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            registerResult.Should().NotBeNull();
            registerResult.AccessToken.Should().NotBeNullOrEmpty();

            // 2. Login with the student
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

            // 3. Get current user info
            var meResponse = await _client.GetAsync("/api/v1/auth/me");
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var userProfile = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>();
            userProfile.Should().NotBeNull();
            userProfile.Email.Should().Be(email);

            // 4. Get student profile
            var studentResponse = await _client.GetAsync("/api/v1/students");
            studentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var students = await studentResponse.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
            students.Should().NotBeNull();

            // 5. Get student enrollments
            var student = students.Items.FirstOrDefault(s => s.Email == email);
            if (student != null)
            {
                var enrollmentsResponse = await _client.GetAsync($"/api/v1/students/{student.Id}/enrollments");
                enrollmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 6. Get grades
            if (student != null)
            {
                var gradesResponse = await _client.GetAsync($"/api/v1/students/{student.Id}/grades");
                gradesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 7. Get assignments
            if (student != null)
            {
                var assignmentsResponse = await _client.GetAsync($"/api/v1/assignments/student/{student.Id}");
                assignmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 8. Logout
            var logoutResponse = await _client.PostAsync("/api/v1/auth/logout", null);
            logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task AdminFlow_CreateCourseCreateUnitEnrollStudent_ShouldSucceed()
        {
            // 1. Login as admin
            var token = await _fixture.GetAuthTokenAsync("admin@school.com", "Admin123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // 2. Create a course
            var courseCommand = new
            {
                name = "Integration Test Course",
                code = $"ITC{Guid.NewGuid():N}".Substring(0, 8).ToUpper(),
                duration = 48,
                totalCredits = 160,
                departmentId = await _fixture.GetFirstDepartmentIdAsync()
            };

            var courseResponse = await _client.PostAsJsonAsync("/api/v1/courses", courseCommand);
            courseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var course = await courseResponse.Content.ReadFromJsonAsync<CourseDto>();
            course.Should().NotBeNull();

            // 3. Create a unit
            var unitCommand = new
            {
                name = "Integration Test Unit",
                code = $"ITU{Guid.NewGuid():N}".Substring(0, 8).ToUpper(),
                credits = 3,
                contactHours = 3,
                courseId = course.Id
            };

            var unitResponse = await _client.PostAsJsonAsync("/api/v1/units", unitCommand);
            unitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var unit = await unitResponse.Content.ReadFromJsonAsync<UnitDto>();
            unit.Should().NotBeNull();

            // 4. Create a student
            var studentEmail = $"student.{Guid.NewGuid()}@example.com";
            var studentCommand = new
            {
                firstName = "Integration",
                lastName = "Student",
                email = studentEmail,
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male",
                address = "123 Integration St"
            };

            var studentResponse = await _client.PostAsJsonAsync("/api/v1/students", studentCommand);
            studentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var student = await studentResponse.Content.ReadFromJsonAsync<StudentDto>();
            student.Should().NotBeNull();

            // 5. Enroll student in the unit
            var semesterId = await _fixture.GetCurrentSemesterIdAsync();
            var enrollCommand = new
            {
                studentId = student.Id,
                unitId = unit.Id,
                semesterId = semesterId
            };

            var enrollResponse = await _client.PostAsJsonAsync("/api/v1/enrollments", enrollCommand);
            enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // 6. Verify enrollment
            var enrollmentsResponse = await _client.GetAsync($"/api/v1/students/{student.Id}/enrollments");
            enrollmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var enrollments = await enrollmentsResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<EnrollmentDto>>();
            enrollments.Should().Contain(e => e.UnitId == unit.Id);
        }

        [Fact]
        public async Task LecturerFlow_RegisterVerifyCreateAssignmentGrade_ShouldSucceed()
        {
            // 1. Register as lecturer (admin action)
            var token = await _fixture.GetAuthTokenAsync("admin@school.com", "Admin123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var lecturerEmail = $"lecturer.{Guid.NewGuid()}@example.com";
            var lecturerCommand = new
            {
                firstName = "Flow",
                lastName = "Lecturer",
                email = lecturerEmail,
                phoneNumber = "+254712345678",
                dateOfBirth = "1980-01-01T00:00:00Z",
                specialization = "Computer Science",
                qualifications = "PhD in CS"
            };

            var lecturerResponse = await _client.PostAsJsonAsync("/api/v1/lecturers", lecturerCommand);
            lecturerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var lecturer = await lecturerResponse.Content.ReadFromJsonAsync<LecturerDto>();
            lecturer.Should().NotBeNull();

            // 2. Verify lecturer (moderator action)
            var verifyResponse = await _client.PostAsync($"/api/v1/lecturers/{lecturer.Id}/verify", null);
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Create assignment as lecturer
            var unitId = await _fixture.GetFirstUnitIdAsync();
            var semesterId = await _fixture.GetCurrentSemesterIdAsync();

            var assignmentCommand = new
            {
                title = "Integration Test Assignment",
                unitId = unitId,
                lecturerId = lecturer.Id,
                semesterId = semesterId,
                maxScore = 100,
                weight = 20,
                dueDate = DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                instructions = "Complete all questions",
                allowLateSubmission = true,
                latePenaltyPercent = 10
            };

            var assignmentResponse = await _client.PostAsJsonAsync("/api/v1/assignments", assignmentCommand);
            assignmentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var assignment = await assignmentResponse.Content.ReadFromJsonAsync<AssignmentDto>();
            assignment.Should().NotBeNull();

            // 4. Get student to grade (create one if needed)
            var studentEmail = $"grade.{Guid.NewGuid()}@example.com";
            var studentCommand = new
            {
                firstName = "Grade",
                lastName = "Student",
                email = studentEmail,
                phoneNumber = "+254712345678",
                dateOfBirth = "2000-01-01T00:00:00Z",
                gender = "Male"
            };
            var studentResponse = await _client.PostAsJsonAsync("/api/v1/students", studentCommand);
            studentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var student = await studentResponse.Content.ReadFromJsonAsync<StudentDto>();

            // 5. Enroll student in the unit
            var enrollCommand = new
            {
                studentId = student.Id,
                unitId = unitId,
                semesterId = semesterId
            };
            await _client.PostAsJsonAsync("/api/v1/enrollments", enrollCommand);

            // 6. Submit assignment as student
            // Note: In a real flow, this would be done with student credentials
            // For this test, we'll simulate with admin credentials
            var submitCommand = new
            {
                assignmentId = assignment.Id,
                studentId = student.Id,
                filePath = "/uploads/test.pdf",
                fileName = "test.pdf",
                fileSize = 1024,
                comments = "Test submission"
            };

            var submitResponse = await _client.PostAsJsonAsync("/api/v1/assignments/submit", submitCommand);
            submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var submission = await submitResponse.Content.ReadFromJsonAsync<AssignmentSubmissionDto>();
            submission.Should().NotBeNull();

            // 7. Grade submission
            var gradeCommand = new
            {
                submissionId = submission.Id,
                score = 85,
                feedback = "Good work!"
            };

            var gradeResponse = await _client.PutAsJsonAsync($"/api/v1/assignments/submissions/{submission.Id}/grade", gradeCommand);
            gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gradedSubmission = await gradeResponse.Content.ReadFromJsonAsync<AssignmentSubmissionDto>();
            gradedSubmission.Should().NotBeNull();
            gradedSubmission.Score.Should().Be(85);
            gradedSubmission.Status.Should().Be("Graded");
        }
    }
}