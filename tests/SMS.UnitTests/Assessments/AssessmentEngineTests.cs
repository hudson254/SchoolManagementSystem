using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using Xunit;

// Aliases to resolve ambiguity
using EnterMarkCmd = SMS.Application.Features.Assessments.Commands.EnterMarkCommand;
using SubmitReviewCmd = SMS.Application.Features.Assessments.Commands.SubmitForReviewCommand;
using PublishResultsCmd = SMS.Application.Features.Assessments.Commands.PublishResultsCommand;
using MarkDto = SMS.Application.Features.Assessments.DTOs.StudentAssessmentMarkDto;

namespace SMS.UnitTests.Assessments
{
    public class AssessmentEngineTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IAssessmentRepository> _assessmentRepositoryMock;
        private readonly Mock<IStudentAssessmentMarkRepository> _markRepositoryMock;
        private readonly Mock<IGradeRepository> _gradeRepositoryMock;
        private readonly Mock<IGradingScaleRepository> _gradingScaleRepositoryMock;
        private readonly Mock<IGradeBandRepository> _gradeBandRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<AssessmentEngine>> _loggerMock;
        private readonly Mock<SMS.Application.Common.Interfaces.ICurrentUserService> _currentUserServiceMock;
        private readonly AssessmentEngine _engine;

        public AssessmentEngineTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _assessmentRepositoryMock = new Mock<IAssessmentRepository>();
            _markRepositoryMock = new Mock<IStudentAssessmentMarkRepository>();
            _gradeRepositoryMock = new Mock<IGradeRepository>();
            _gradingScaleRepositoryMock = new Mock<IGradingScaleRepository>();
            _gradeBandRepositoryMock = new Mock<IGradeBandRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<AssessmentEngine>>();
            _currentUserServiceMock = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();

            _engine = new AssessmentEngine(
                _mediatorMock.Object,
                _assessmentRepositoryMock.Object,
                _markRepositoryMock.Object,
                _gradeRepositoryMock.Object,
                _gradingScaleRepositoryMock.Object,
                _gradeBandRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object,
                _currentUserServiceMock.Object
            );
        }

        #region Weight Validation Tests

        [Fact]
        public async Task ValidateWeightsAsync_ValidWeights_ReturnsValid()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Title = "Assignment 1", Weight = 30, IsActive = true },
                new Assessment { Id = Guid.NewGuid(), Title = "Final Exam", Weight = 70, IsActive = true }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);

            // Act
            var result = await _engine.ValidateWeightsAsync(unitId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(100, result.TotalWeight);
            Assert.Equal(2, result.Weights.Count);
        }

        [Fact]
        public async Task ValidateWeightsAsync_InvalidWeights_ReturnsInvalid()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Title = "Assignment 1", Weight = 30, IsActive = true },
                new Assessment { Id = Guid.NewGuid(), Title = "Final Exam", Weight = 60, IsActive = true }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);

            // Act
            var result = await _engine.ValidateWeightsAsync(unitId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(90, result.TotalWeight);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task ValidateWeightsAsync_Over100Percent_ReturnsInvalid()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Title = "Assignment 1", Weight = 60, IsActive = true },
                new Assessment { Id = Guid.NewGuid(), Title = "Final Exam", Weight = 50, IsActive = true }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);

            // Act
            var result = await _engine.ValidateWeightsAsync(unitId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(110, result.TotalWeight);
            Assert.Single(result.Errors);
        }

        #endregion

        #region Mark Entry Tests

        [Fact]
        public async Task EnterMarkAsync_ValidMark_ReturnsMarkDto()
        {
            // Arrange
            var assessmentId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var command = new EnterMarkCommand
            {
                AssessmentId = assessmentId,
                StudentId = studentId,
                Score = 85,
                MaxScore = 100,
                Feedback = "Good work"
            };

            var assessment = new Assessment
            {
                Id = assessmentId,
                Title = "Assignment 1",
                Weight = 30,
                MaxScore = 100,
                IsWeightLocked = false,
                UnitId = Guid.NewGuid()
            };

            var mark = new StudentAssessmentMark
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = studentId,
                Mark = 85,
                Percentage = 85,
                WeightedScore = 25.5m
            };

            _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(assessmentId))
                .ReturnsAsync(assessment);
            _mediatorMock.Setup(m => m.Send(It.IsAny<EnterMarkCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentAssessmentMarkDto
                {
                    Id = mark.Id,
                    AssessmentId = mark.AssessmentId,
                    StudentId = mark.StudentId,
                    Score = mark.Mark,
                    Percentage = mark.Percentage,
                    WeightedScore = mark.WeightedScore
                });

            // Act
            var result = await _engine.EnterMarkAsync(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(85, result.Score);
            Assert.Equal(25.5m, result.WeightedScore);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EnterMarkAsync_ScoreOutOfRange_ThrowsException()
        {
            // Arrange
            var command = new EnterMarkCommand
            {
                AssessmentId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Score = 150,
                MaxScore = 100
            };

            // Need to mock assessment repository first - otherwise KeyNotFoundException is thrown
            _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(command.AssessmentId))
                .ReturnsAsync(new Assessment
                {
                    Id = command.AssessmentId,
                    UnitId = Guid.NewGuid(),
                    IsWeightLocked = false,
                    Title = "Test Assessment",
                    MaxScore = 100
                });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.EnterMarkAsync(command));
        }

        [Fact]
        public async Task EnterMarkAsync_LockedAssessment_ThrowsException()
        {
            // Arrange
            var assessmentId = Guid.NewGuid();
            var command = new EnterMarkCommand
            {
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                Score = 85,
                MaxScore = 100
            };

            var assessment = new Assessment
            {
                Id = assessmentId,
                IsWeightLocked = true
            };

            _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(assessmentId))
                .ReturnsAsync(assessment);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.EnterMarkAsync(command));
        }

        #endregion

        #region Calculation Tests

        [Fact]
        public async Task CalculateStudentResultAsync_ValidMarks_ReturnsCorrectResult()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Title = "Assignment 1", Weight = 30, MaxScore = 100, IsActive = true },
                new Assessment { Id = Guid.NewGuid(), Title = "Final Exam", Weight = 70, MaxScore = 100, IsActive = true }
            };

            var marks = new List<StudentAssessmentMark>
            {
                new StudentAssessmentMark
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessments[0].Id,
                    StudentId = studentId,
                    Mark = 80,
                    Percentage = 80,
                    WeightedScore = 24,
                    IsExempt = false
                },
                new StudentAssessmentMark
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessments[1].Id,
                    StudentId = studentId,
                    Mark = 90,
                    Percentage = 90,
                    WeightedScore = 63,
                    IsExempt = false
                }
            };

            var gradingScale = new GradingScale
            {
                Id = Guid.NewGuid(),
                Name = "Default Scale",
                IsActive = true
            };

            var gradeBands = new List<GradeBand>
            {
                new GradeBand { Id = Guid.NewGuid(), GradingScaleId = gradingScale.Id, MinPercentage = 75, MaxPercentage = 100, GradeLetter = "A", Description = "Distinction" },
                new GradeBand { Id = Guid.NewGuid(), GradingScaleId = gradingScale.Id, MinPercentage = 65, MaxPercentage = 74.99m, GradeLetter = "B", Description = "Credit" },
                new GradeBand { Id = Guid.NewGuid(), GradingScaleId = gradingScale.Id, MinPercentage = 50, MaxPercentage = 64.99m, GradeLetter = "C", Description = "Pass" },
                new GradeBand { Id = Guid.NewGuid(), GradingScaleId = gradingScale.Id, MinPercentage = 0, MaxPercentage = 49.99m, GradeLetter = "F", Description = "Fail" }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, studentId))
                .ReturnsAsync(marks);
            _gradingScaleRepositoryMock.Setup(r => r.GetActiveVersionAsync())
                .ReturnsAsync(gradingScale);
            _gradeBandRepositoryMock.Setup(r => r.GetByScaleAsync(gradingScale.Id))
                .ReturnsAsync(gradeBands);

            // Act
            var result = await _engine.CalculateStudentResultAsync(unitId, studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(87, result.FinalScore); // (80 * 0.3) + (90 * 0.7) = 24 + 63 = 87
            Assert.Equal("A", result.FinalGrade);
            Assert.Equal("Distinction", result.GradeDescription);
            Assert.True(result.IsPassed);
        }

        [Fact]
        public async Task CalculateStudentResultAsync_ExemptAssessment_ExcludesFromCalculation()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Title = "Assignment 1", Weight = 30, MaxScore = 100, IsActive = true },
                new Assessment { Id = Guid.NewGuid(), Title = "Final Exam", Weight = 70, MaxScore = 100, IsActive = true }
            };

            var marks = new List<StudentAssessmentMark>
            {
                new StudentAssessmentMark
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessments[0].Id,
                    StudentId = studentId,
                    Mark = 80,
                    Percentage = 80,
                    WeightedScore = 24,
                    IsExempt = true // Exempt
                },
                new StudentAssessmentMark
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessments[1].Id,
                    StudentId = studentId,
                    Mark = 90,
                    Percentage = 90,
                    WeightedScore = 90,
                    IsExempt = false
                }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, studentId))
                .ReturnsAsync(marks);

            // Act
            var result = await _engine.CalculateStudentResultAsync(unitId, studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(90, result.FinalScore); // Only final exam counts
            Assert.Equal(70, result.TotalWeight); // Only 70% weight
        }

        #endregion

        #region Grade Assignment Tests

        [Fact]
        public async Task AssignGradeAsync_ScoreInRange_AssignsCorrectGrade()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Title = "Final Exam", Weight = 100, MaxScore = 100, IsActive = true }
            };

            var marks = new List<StudentAssessmentMark>
            {
                new StudentAssessmentMark
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessments[0].Id,
                    StudentId = studentId,
                    Mark = 78,
                    Percentage = 78,
                    WeightedScore = 78,
                    IsExempt = false
                }
            };

            var gradingScale = new GradingScale
            {
                Id = Guid.NewGuid(),
                Name = "Default Scale",
                IsActive = true
            };

            var gradeBands = new List<GradeBand>
            {
                new GradeBand { Id = Guid.NewGuid(), GradingScaleId = gradingScale.Id, MinPercentage = 75, MaxPercentage = 100, GradeLetter = "A", Description = "Distinction" },
                new GradeBand { Id = Guid.NewGuid(), GradingScaleId = gradingScale.Id, MinPercentage = 65, MaxPercentage = 74.99m, GradeLetter = "B", Description = "Credit" }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, Guid.Empty))
                .ReturnsAsync(marks);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, studentId))
                .ReturnsAsync(marks);
            _gradingScaleRepositoryMock.Setup(r => r.GetActiveVersionAsync())
                .ReturnsAsync(gradingScale);
            _gradeBandRepositoryMock.Setup(r => r.GetByScaleAsync(gradingScale.Id))
                .ReturnsAsync(gradeBands);

            // Set up existing grade so UpdateAsync is called
            var existingGrade = new Grade
            {
                StudentId = studentId,
                UnitId = unitId,
                Score = 0,
                LetterGrade = "F",
                Remarks = "Initial"
            };
            _gradeRepositoryMock.Setup(r => r.GetStudentGradesAsync(studentId))
                .ReturnsAsync(new List<Grade> { existingGrade });

            // Act
            await _engine.AssignGradesAsync(unitId);

            // Assert
            _gradeRepositoryMock.Verify(g => g.UpdateAsync(It.IsAny<Grade>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Publication Workflow Tests

        [Fact]
        public async Task SubmitForReviewAsync_ValidWeights_UpdatesStatus()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var command = new SubmitForReviewCommand { UnitId = unitId };

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Weight = 50, PublicationStatus = ResultPublicationStatus.Draft },
                new Assessment { Id = Guid.NewGuid(), Weight = 50, PublicationStatus = ResultPublicationStatus.Draft }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);

            // Act
            await _engine.SubmitForReviewAsync(command);

            // Assert
            _assessmentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Assessment>()), Times.Exactly(2));
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _auditServiceMock.Verify(a => a.LogActivityAsync("Results", "SubmittedForReview", unitId.ToString(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SubmitForReviewAsync_InvalidWeights_ThrowsException()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var command = new SubmitForReviewCommand { UnitId = unitId };

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), Weight = 30, PublicationStatus = ResultPublicationStatus.Draft },
                new Assessment { Id = Guid.NewGuid(), Weight = 30, PublicationStatus = ResultPublicationStatus.Draft }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.SubmitForReviewAsync(command));
        }

        [Fact]
        public async Task PublishResultsAsync_ApprovedStatus_PublishesResults()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var command = new PublishResultsCommand { UnitId = unitId };

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), PublicationStatus = ResultPublicationStatus.Approved, UnitId = unitId },
                new Assessment { Id = Guid.NewGuid(), PublicationStatus = ResultPublicationStatus.Approved, UnitId = unitId }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, Guid.Empty))
                .ReturnsAsync(new List<StudentAssessmentMark>());

            // Act
            await _engine.PublishResultsAsync(command);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _auditServiceMock.Verify(a => a.LogActivityAsync("Results", "Published", null, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Certificate Eligibility Tests

        [Fact]
        public async Task CheckCertificateEligibilityAsync_AllConditionsMet_ReturnsTrue()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var unitId = Guid.NewGuid();

            var assessmentId = Guid.NewGuid();
            var assessments = new List<Assessment>
            {
                new Assessment { Id = assessmentId, IsMandatory = true, IsActive = true, Weight = 100, MaxScore = 100, Title = "Final Exam" }
            };

            var marks = new List<StudentAssessmentMark>
            {
                new StudentAssessmentMark
                {
                    AssessmentId = assessmentId,
                    StudentId = studentId,
                    Mark = 80,
                    Percentage = 80,
                    WeightedScore = 80,
                    IsDraft = false,
                    IsExempt = false
                }
            };

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, studentId))
                .ReturnsAsync(marks);

            // Act
            var result = await _engine.CheckCertificateEligibilityAsync(studentId, unitId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckCertificateEligibilityAsync_MissingMandatoryAssessment_ReturnsFalse()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var unitId = Guid.NewGuid();

            var assessments = new List<Assessment>
            {
                new Assessment { Id = Guid.NewGuid(), IsMandatory = true }
            };

            var marks = new List<StudentAssessmentMark>(); // No marks

            _assessmentRepositoryMock.Setup(r => r.GetByUnitAsync(unitId))
                .ReturnsAsync(assessments);
            _markRepositoryMock.Setup(r => r.GetByUnitAndStudentAsync(unitId, studentId))
                .ReturnsAsync(marks);

            // Act
            var result = await _engine.CheckCertificateEligibilityAsync(studentId, unitId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Security Tests

        [Fact]
        public async Task EnterMarkAsync_NegativeScore_ThrowsException()
        {
            // Arrange
            var command = new EnterMarkCommand
            {
                AssessmentId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Score = -10,
                MaxScore = 100
            };

            // Need to mock assessment repository first - otherwise KeyNotFoundException is thrown
            _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(command.AssessmentId))
                .ReturnsAsync(new Assessment
                {
                    Id = command.AssessmentId,
                    UnitId = Guid.NewGuid(),
                    IsWeightLocked = false,
                    Title = "Test Assessment",
                    MaxScore = 100
                });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.EnterMarkAsync(command));
        }

        [Fact]
        public async Task UpdateAssessmentWeightAsync_WeightOutOfRange_ThrowsException()
        {
            // Arrange
            var assessmentId = Guid.NewGuid();
            var newWeight = 150m;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _engine.UpdateAssessmentWeightAsync(assessmentId, newWeight));
        }

        #endregion
    }
}

