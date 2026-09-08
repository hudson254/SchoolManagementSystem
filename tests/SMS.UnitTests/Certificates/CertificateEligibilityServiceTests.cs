using Microsoft.Extensions.Logging;
using Moq;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Certificates.Infrastructure.Services;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Certificates
{
    public class CertificateEligibilityServiceTests
    {
        private readonly Mock<ICourseOfferingRepository> _offeringRepo;
        private readonly Mock<IEnrollmentRepository> _enrollmentRepo;
        private readonly Mock<IAssessmentEngine> _assessmentEngine;
        private readonly Mock<IUnitResultRepository> _unitResultRepo;
        private readonly Mock<IAssignmentRepository> _assignmentRepo;
        private readonly Mock<ICertificateRepository> _certRepo;
        private readonly Mock<ICertificateRuleRepository> _ruleRepo;
        private readonly Mock<ILogger<CertificateEligibilityService>> _logger;
        private readonly CertificateEligibilityService _service;

        public CertificateEligibilityServiceTests()
        {
            _offeringRepo = new Mock<ICourseOfferingRepository>();
            _enrollmentRepo = new Mock<IEnrollmentRepository>();
            _assessmentEngine = new Mock<IAssessmentEngine>();
            _unitResultRepo = new Mock<IUnitResultRepository>();
            _assignmentRepo = new Mock<IAssignmentRepository>();
            _certRepo = new Mock<ICertificateRepository>();
            _ruleRepo = new Mock<ICertificateRuleRepository>();
            _logger = new Mock<ILogger<CertificateEligibilityService>>();
            _service = new CertificateEligibilityService(
                _offeringRepo.Object,
                _enrollmentRepo.Object,
                _assessmentEngine.Object,
                _unitResultRepo.Object,
                _assignmentRepo.Object,
                _certRepo.Object,
                _ruleRepo.Object,
                _logger.Object);
        }

        private void MockEligibleEngine(Guid studentId)
        {
            _assessmentEngine.Setup(e => e.EvaluateCertificateEligibilityAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentCertificateEligibility { StudentId = studentId, Status = CertificateEligibilityStatus.Eligible });
        }

        private void MockPublishedResult(Guid studentId, string letter, decimal percentage)
        {
            _unitResultRepo.Setup(r => r.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UnitResult> { new() { StudentId = studentId, GradeLetter = letter, FinalPercentage = percentage, IsPublished = true } });
        }

        private void MockNoResults(Guid studentId)
        {
            _unitResultRepo.Setup(r => r.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UnitResult>());
        }

        private void SetupCompletedEnrolledOffering(Guid studentId, Guid offeringId)
        {
            _offeringRepo.Setup(r => r.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering { Status = CourseOfferingStatus.Completed });
            _enrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Enrollment());
            _certRepo.Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Certificate>());
        }

        private void SetupDefaultRule()
        {
            _ruleRepo.Setup(r => r.GetActiveRuleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CertificateRule { MinimumPassingGradeLetter = "D", RequireAllMandatoryAssessments = false, RequireNoOutstandingIncomplete = false });
        }
[Fact]
        public async Task CheckEligibility_CompletedCourse_EnrolledStudent_WithGrade_ReturnsEligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            MockEligibleEngine(studentId);
            MockPublishedResult(studentId, "A", 85m);
            SetupDefaultRule();

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.True(result.IsEligible);
            Assert.Equal("A", result.FinalGrade);
            Assert.Equal("Distinction", result.Classification);
        }

        [Fact]
        public async Task CheckEligibility_CourseNotCompleted_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            _offeringRepo.Setup(r => r.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering { Status = CourseOfferingStatus.Active });

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Contains("Course has not been marked as completed", result.IneligibilityReasons);
        }

        [Fact]
        public async Task CheckEligibility_StudentNotEnrolled_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            _offeringRepo.Setup(r => r.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering { Status = CourseOfferingStatus.Completed });
            _enrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enrollment?)null);

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Contains("Student is not enrolled in this course offering", result.IneligibilityReasons);
        }

        [Fact]
        public async Task CheckEligibility_CertificateAlreadyIssued_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            _certRepo.Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Certificate> { new() { CourseOfferingId = offeringId, Status = CertificateStatus.Issued } });

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Contains("Certificate has already been issued for this course", result.IneligibilityReasons);
        }

        [Fact]
        public async Task CheckEligibility_NoPublishedResults_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            MockEligibleEngine(studentId);
            MockNoResults(studentId);

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Contains("No published unit results recorded for student", result.IneligibilityReasons);
        }

        [Fact]
        public async Task CheckEligibility_EngineNotEligible_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            _assessmentEngine.Setup(e => e.EvaluateCertificateEligibilityAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentCertificateEligibility { StudentId = studentId, Status = CertificateEligibilityStatus.NotEligible });

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Equal("Authoritative eligibility check failed - see StudentCertificateEligibility", Assert.Single(result.IneligibilityReasons));
        }
[Fact]
        public async Task CheckEligibility_FinalGradeBelowPassing_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            MockEligibleEngine(studentId);
            _unitResultRepo.Setup(r => r.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UnitResult> { new() { StudentId = studentId, GradeLetter = "F", FinalPercentage = 30m, IsPublished = true } });
            _ruleRepo.Setup(r => r.GetActiveRuleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CertificateRule { MinimumPassingGradeLetter = "D", RequireAllMandatoryAssessments = false });

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Contains("Final grade F does not meet minimum passing requirement (minimum D)", result.IneligibilityReasons);
        }

        [Fact]
        public async Task CheckEligibility_MissingMandatoryAssessment_ReturnsIneligible()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            MockEligibleEngine(studentId);
            MockPublishedResult(studentId, "A", 85m);
            _ruleRepo.Setup(r => r.GetActiveRuleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CertificateRule { MinimumPassingGradeLetter = "D", RequireAllMandatoryAssessments = true });
            _assignmentRepo.Setup(r => r.GetAssignmentsByStudentAsync(studentId))
                .ReturnsAsync(new List<Assignment> { new() { IsActive = true } });
            _assignmentRepo.Setup(r => r.HasSubmissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.False(result.IsEligible);
            Assert.Contains("Not all mandatory assessments have been completed", result.IneligibilityReasons);
        }

        [Fact]
        public async Task CheckEligibility_ClassificationCalculation_ReturnsCorrectClassification()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            SetupCompletedEnrolledOffering(studentId, offeringId);
            MockEligibleEngine(studentId);
            MockPublishedResult(studentId, "B", 75m);
            SetupDefaultRule();

            // Act
            var result = await _service.CheckEligibilityAsync(studentId, offeringId);

            // Assert
            Assert.True(result.IsEligible);
            Assert.Equal("B", result.FinalGrade);
            Assert.Equal("Merit", result.Classification);
        }
[Fact]
        public async Task CheckBulkEligibility_ReturnsResultsForAllEnrolledStudents()
        {
            // Arrange
            var offeringId = Guid.NewGuid();
            var student1 = Guid.NewGuid();
            var student2 = Guid.NewGuid();
            _enrollmentRepo.Setup(r => r.GetEnrollmentsByCourseAsync(offeringId))
                .ReturnsAsync(new List<Enrollment> { new() { StudentId = student1 }, new() { StudentId = student2 } });
            _enrollmentRepo.Setup(r => r.GetEnrollmentAsync(student1, offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Enrollment());
            _enrollmentRepo.Setup(r => r.GetEnrollmentAsync(student2, offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Enrollment());
            _offeringRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering { Status = CourseOfferingStatus.Completed });
            _certRepo.Setup(r => r.GetByStudentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Certificate>());
            _assessmentEngine.Setup(e => e.EvaluateCertificateEligibilityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentCertificateEligibility { Status = CertificateEligibilityStatus.Eligible });
            _unitResultRepo.Setup(r => r.GetByStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UnitResult> { new() { GradeLetter = "A", FinalPercentage = 85m, IsPublished = true } });
            SetupDefaultRule();

            // Act
            var results = await _service.CheckBulkEligibilityAsync(offeringId);

            // Assert
            Assert.Equal(2, results.Count());
            Assert.All(results, r => Assert.True(r.IsEligible));
        }

        [Fact]
        public async Task GetEligibleStudents_ReturnsOnlyEligibleStudentIds()
        {
            // Arrange
            var offeringId = Guid.NewGuid();
            var eligibleStudent = Guid.NewGuid();
            var ineligibleStudent = Guid.NewGuid();
            _enrollmentRepo.Setup(r => r.GetEnrollmentsByCourseAsync(offeringId))
                .ReturnsAsync(new List<Enrollment> { new() { StudentId = eligibleStudent }, new() { StudentId = ineligibleStudent } });
            _enrollmentRepo.Setup(r => r.GetEnrollmentAsync(eligibleStudent, offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Enrollment());
            _enrollmentRepo.Setup(r => r.GetEnrollmentAsync(ineligibleStudent, offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Enrollment());
            _offeringRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering { Status = CourseOfferingStatus.Completed });
            _certRepo.Setup(r => r.GetByStudentIdAsync(eligibleStudent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Certificate>());
            _certRepo.Setup(r => r.GetByStudentIdAsync(ineligibleStudent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Certificate> { new() { CourseOfferingId = offeringId, Status = CertificateStatus.Issued } });
            _assessmentEngine.Setup(e => e.EvaluateCertificateEligibilityAsync(eligibleStudent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentCertificateEligibility { StudentId = eligibleStudent, Status = CertificateEligibilityStatus.Eligible });
            _unitResultRepo.Setup(r => r.GetByStudentAsync(eligibleStudent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UnitResult> { new() { StudentId = eligibleStudent, GradeLetter = "A", FinalPercentage = 85m, IsPublished = true } });
            _unitResultRepo.Setup(r => r.GetByStudentAsync(ineligibleStudent, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UnitResult> { new() { StudentId = ineligibleStudent, GradeLetter = "A", FinalPercentage = 85m, IsPublished = true } });
            _ruleRepo.Setup(r => r.GetActiveRuleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CertificateRule { MinimumPassingGradeLetter = "D", RequireAllMandatoryAssessments = false, RequireNoOutstandingIncomplete = false });

            // Act
            var eligibleIds = await _service.GetEligibleStudentsAsync(offeringId);

            // Assert
            Assert.Single(eligibleIds);
            Assert.Equal(eligibleStudent, eligibleIds.First());
        }
    }
}
