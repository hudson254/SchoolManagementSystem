using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.Queries;
using System.Text;

namespace SMS.Application.Services
{
    /// <summary>
    /// Centralized Assessment Engine - Orchestrates all assessment, grading, and calculation operations.
    /// This is the single point of control for all assessment-related business logic.
    /// </summary>
    public class AssessmentEngine : SMS.Domain.Interfaces.IAssessmentEngine
    {
        private readonly IMediator _mediator;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly IGradingScaleRepository _gradingScaleRepository;
        private readonly IGradeBandRepository _gradeBandRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssessmentEngine> _logger;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;

        public AssessmentEngine(
            IMediator mediator,
            IAssessmentRepository assessmentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IGradeRepository gradeRepository,
            IGradingScaleRepository gradingScaleRepository,
            IGradeBandRepository gradeBandRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AssessmentEngine> logger,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _assessmentRepository = assessmentRepository;
            _markRepository = markRepository;
            _gradeRepository = gradeRepository;
            _gradingScaleRepository = gradingScaleRepository;
            _gradeBandRepository = gradeBandRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        #region Assessment Management

        public async Task<AssessmentDto> CreateAssessmentAsync(CreateAssessmentCommand command)
        {
            _logger.LogInformation("Creating assessment: {AssessmentName} for unit {UnitId}",
                command.Name, command.UnitId);

            // Validate weight
            if (command.Weight < 0 || command.Weight > 100)
            {
                throw new InvalidOperationException("Assessment weight must be between 0 and 100.");
            }

            // Create assessment via command handler
            var assessment = await _mediator.Send(command);

            // Audit log
            await _auditService.LogActivityAsync("Assessment", "Created", assessment.Id.ToString(),
                $"Created assessment: {assessment.Name} with weight {command.Weight}%");

            return assessment;
        }

        public async Task<AssessmentDto> UpdateAssessmentAsync(Guid assessmentId, UpdateAssessmentCommand command)
        {
            _logger.LogInformation("Updating assessment: {AssessmentId}", assessmentId);

            var existingAssessment = await _assessmentRepository.GetByIdAsync(assessmentId);
            if (existingAssessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            // Check if weight is locked
            if (existingAssessment.IsWeightLocked)
            {
                throw new InvalidOperationException("Assessment weight is locked. Unlock before modifying.");
            }

            // Check if marks have been entered
            var hasMarks = await _markRepository.CountGradedAsync(assessmentId) > 0;
            if (hasMarks && command.Weight != existingAssessment.Weight)
            {
                throw new InvalidOperationException("Cannot change weight after marks have been entered.");
            }

            command.Id = assessmentId;

            // Use mediator if handler exists, otherwise update directly
            AssessmentDto assessment;
            try
            {
                assessment = await _mediator.Send(command);
            }
            catch
            {
                // Fallback: update directly
                existingAssessment.Title = command.Name;
                existingAssessment.Description = command.Description;
                existingAssessment.Weight = command.Weight;
                await _assessmentRepository.UpdateAsync(existingAssessment);
                await _unitOfWork.SaveChangesAsync();

                assessment = new AssessmentDto
                {
                    Id = existingAssessment.Id,
                    Name = existingAssessment.Title,
                    Description = existingAssessment.Description,
                    Weight = existingAssessment.Weight
                };
            }

            // Audit log
            await _auditService.LogActivityAsync("Assessment", "Updated", assessmentId.ToString(),
                $"Updated assessment: {assessment.Name}. New weight: {command.Weight}%");

            return assessment;
        }

        public async Task DeleteAssessmentAsync(Guid assessmentId)
        {
            _logger.LogInformation("Deleting assessment: {AssessmentId}", assessmentId);

            var assessment = await _assessmentRepository.GetByIdAsync(assessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            // Check if marks exist
            var hasMarks = await _markRepository.CountGradedAsync(assessmentId) > 0;
            if (hasMarks)
            {
                throw new InvalidOperationException("Cannot delete assessment with existing marks.");
            }

            var assessmentTitle = assessment.Title;
            await _assessmentRepository.DeleteAsync(assessment);
            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Assessment", "Deleted", assessmentId.ToString(),
                $"Deleted assessment: {assessmentTitle}");
        }

        #endregion

        #region Weight Management

        public async Task<WeightValidationResult> ValidateWeightsAsync(Guid unitId)
        {
            _logger.LogInformation("Validating weights for unit: {UnitId}", unitId);

            var assessments = await _assessmentRepository.GetByUnitAsync(unitId);
            var result = new WeightValidationResult
            {
                TotalWeight = assessments.Sum(a => a.Weight),
                Weights = assessments.Select(a => new AssessmentWeightDto
                {
                    AssessmentId = a.Id,
                    AssessmentName = a.Title,
                    Weight = a.Weight
                }).ToList()
            };

            // Validate total equals 100%
            if (result.TotalWeight != 100)
            {
                result.IsValid = false;
                result.Errors.Add(new WeightValidationError
                {
                    Message = $"Total weight is {result.TotalWeight}%. Must equal exactly 100%.",
                    Field = "TotalWeight"
                });
            }

            // Validate individual weights
            foreach (var assessment in assessments)
            {
                if (assessment.Weight < 0 || assessment.Weight > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add(new WeightValidationError
                    {
                        Message = $"Assessment '{assessment.Title}' has invalid weight: {assessment.Weight}%",
                        Field = $"Assessment_{assessment.Id}"
                    });
                }
            }

            return result;
        }

        public async Task UpdateAssessmentWeightAsync(Guid assessmentId, decimal newWeight)
        {
            _logger.LogInformation("Updating weight for assessment: {AssessmentId} to {Weight}%",
                assessmentId, newWeight);

            if (newWeight < 0 || newWeight > 100)
            {
                throw new InvalidOperationException("Weight must be between 0 and 100.");
            }

            var assessment = await _assessmentRepository.GetByIdAsync(assessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            if (assessment.IsWeightLocked)
            {
                throw new InvalidOperationException("Assessment weight is locked.");
            }

            var oldWeight = assessment.Weight;
            assessment.Weight = newWeight;
            await _assessmentRepository.UpdateAsync(assessment);
            await _unitOfWork.SaveChangesAsync();

            // Recalculate all marks for this assessment
            await RecalculateResultsAsync(assessment.UnitId);

            // Audit log
            await _auditService.LogActivityAsync("Assessment", "WeightUpdated", assessmentId.ToString(),
                $"Weight changed from {oldWeight}% to {newWeight}% for assessment: {assessment.Title}");
        }

        #endregion

        #region Mark Entry

        public async Task<StudentAssessmentMarkDto> EnterMarkAsync(EnterMarkCommand command)
        {
            _logger.LogInformation("Entering mark for student {StudentId} on assessment {AssessmentId}",
                command.StudentId, command.AssessmentId);

            // Validate assessment
            var assessment = await _assessmentRepository.GetByIdAsync(command.AssessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            // Validate mark range
            if (command.Score < 0 || command.Score > command.MaxScore)
            {
                throw new InvalidOperationException($"Score must be between 0 and {command.MaxScore}.");
            }

            // Check if assessment is locked
            if (assessment.IsWeightLocked)
            {
                throw new InvalidOperationException("Assessment is locked. Cannot enter marks.");
            }

            // Send command to handler
            var mark = await _mediator.Send(command);

            // Recalculate student result
            await RecalculateResultsAsync(assessment.UnitId);

            // Audit log
            await _auditService.LogActivityAsync("Mark", "Entered", mark.Id.ToString(),
                $"Entered mark: {command.Score}/{command.MaxScore} for student {command.StudentId}");

            return mark;
        }

        public async Task<BulkMarkImportResult> ImportMarksAsync(ImportMarksCommand command)
        {
            _logger.LogInformation("Importing {Count} marks for assessment {AssessmentId}",
                command.Records.Count, command.AssessmentId);

            var assessment = await _assessmentRepository.GetByIdAsync(command.AssessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            if (assessment.IsWeightLocked)
            {
                throw new InvalidOperationException("Assessment is locked. Cannot import marks.");
            }

            // Validate all records before importing
            var errors = new List<string>();
            foreach (var record in command.Records)
            {
                if (record.Score < 0 || record.Score > record.MaxScore)
                {
                    errors.Add($"Student {record.StudentId}: Invalid score {record.Score}. Must be 0-{record.MaxScore}.");
                }
            }

            if (errors.Any())
            {
                return new BulkMarkImportResult
                {
                    TotalRecords = command.Records.Count,
                    SuccessCount = 0,
                    ErrorCount = errors.Count,
                    Errors = errors,
                    ImportBatchId = command.ImportBatchId ?? Guid.NewGuid()
                };
            }

            // Import marks
            var result = await _mediator.Send(command);

            // Recalculate all results
            await RecalculateResultsAsync(assessment.UnitId);

            // Audit log
            await _auditService.LogActivityAsync("Mark", "Imported", assessment.Id.ToString(),
                $"Imported {result.SuccessCount} marks for assessment: {assessment.Title}");

            return result;
        }

        public async Task<StudentAssessmentMarkDto> UpdateMarkAsync(UpdateMarkCommand command)
        {
            _logger.LogInformation("Updating mark {MarkId}", command.MarkId);

            var mark = await _markRepository.GetByIdAsync(command.MarkId);
            if (mark == null)
            {
                throw new KeyNotFoundException("Mark not found.");
            }

            var assessment = await _assessmentRepository.GetByIdAsync(mark.AssessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            if (assessment.IsWeightLocked)
            {
                throw new InvalidOperationException("Assessment is locked. Cannot update marks.");
            }

            // Store original mark for audit
            var originalMark = mark.Mark;

            var updatedMark = await _mediator.Send(command);

            // Recalculate results
            await RecalculateResultsAsync(assessment.UnitId);

            // Audit log
            await _auditService.LogActivityAsync("Mark", "Updated", mark.Id.ToString(),
                $"Updated mark from {originalMark} to {command.Score} for student {mark.StudentId}. Reason: {command.Reason}");

            return updatedMark;
        }

        #endregion

        #region Calculations

        public async Task<StudentResultDto> CalculateStudentResultAsync(Guid unitId, Guid studentId)
        {
            _logger.LogInformation("Calculating result for student {StudentId} in unit {UnitId}",
                studentId, unitId);

            var assessments = await _assessmentRepository.GetByUnitAsync(unitId);
            var marks = await _markRepository.GetByUnitAndStudentAsync(unitId, studentId);

            var result = new StudentResultDto
            {
                StudentId = studentId,
                UnitId = unitId,
                AssessmentMarks = new List<StudentAssessmentMarkDto>()
            };

            // Get student and unit names
            // TODO: Fetch from repositories
            result.StudentName = $"Student {studentId}";
            result.UnitName = $"Unit {unitId}";

            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;

            foreach (var assessment in assessments.Where(a => a.IsActive))
            {
                var mark = marks.FirstOrDefault(m => m.AssessmentId == assessment.Id);

                if (mark != null && !mark.IsExempt)
                {
                    // Calculate percentage
                    var percentage = (mark.Mark / assessment.MaxScore) * 100;
                    mark.Percentage = percentage;

                    // Calculate weighted score
                    var weightedScore = percentage * (assessment.Weight / 100);
                    mark.WeightedScore = weightedScore;

                    totalWeightedScore += weightedScore;
                    totalWeight += assessment.Weight;

                    // Add to result
                    result.AssessmentMarks.Add(new StudentAssessmentMarkDto
                    {
                        Id = mark.Id,
                        AssessmentId = mark.AssessmentId,
                        AssessmentName = assessment.Title,
                        StudentId = mark.StudentId,
                        Score = mark.Mark,
                        MaxScore = assessment.MaxScore,
                        Percentage = percentage,
                        WeightedScore = weightedScore,
                        Weight = assessment.Weight,
                        Feedback = mark.Feedback,
                        IsDraft = mark.IsDraft,
                        GradedDate = mark.GradedDate,
                        GradedBy = mark.GradedBy?.ToString(),
                        PublicationStatus = assessment.PublicationStatus,
                        ModerationStatus = assessment.ModerationStatus
                    });
                }
                else if (mark != null && mark.IsExempt)
                {
                    // Exempt - skip this assessment entirely.
                    // Do NOT subtract its weight: the total non-exempt weight is
                    // accumulated only for non-exempt marks, and the final score is
                    // normalized to that total below.
                    continue;
                }
            }

            // Calculate final score.
            // When some assessments are exempt, the remaining assessments' total
            // weight is less than 100%. To keep the final score comparable with
            // the standard 0-100 grading bands, normalize the weighted score to
            // the total non-exempt weight (e.g. a 90% on the only remaining 70%
            // assessment yields 90.00, not 63.00).
            result.FinalScore = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : 0;
            result.TotalWeight = totalWeight;

            // Assign grade based on grading scale
            var gradingScale = await _gradingScaleRepository.GetActiveVersionAsync();
            if (gradingScale != null)
            {
                var gradeBands = await _gradeBandRepository.GetByScaleAsync(gradingScale.Id);
                var gradeBand = gradeBands.FirstOrDefault(b =>
                    result.FinalScore >= b.MinPercentage &&
                    result.FinalScore <= b.MaxPercentage);

                if (gradeBand != null)
                {
                    result.FinalGrade = gradeBand.GradeLetter;
                    result.GradeDescription = gradeBand.Description;
                    result.GradeColor = gradeBand.ColorCode;
                    result.IsPassed = result.FinalScore >= 50; // TODO: Make configurable
                    result.GradingScaleVersionId = gradingScale.Id;
                }
            }

            // Check certificate eligibility
            result.IsEligibleForCertificate = await CheckCertificateEligibilityAsync(studentId, unitId);

            result.IsPublished = result.AssessmentMarks.All(m =>
                m.PublicationStatus == ResultPublicationStatus.Published);

            result.PublicationStatus = result.AssessmentMarks.Any()
                ? result.AssessmentMarks.Max(m => m.PublicationStatus)
                : ResultPublicationStatus.Draft;

            return result;
        }

        public async Task<IEnumerable<StudentResultDto>> CalculateAllResultsAsync(Guid unitId)
        {
            _logger.LogInformation("Calculating all results for unit {UnitId}", unitId);

            // Get all students with marks in this unit
            var marks = await _markRepository.GetByUnitAndStudentAsync(unitId, Guid.Empty);
            var studentIds = marks.Select(m => m.StudentId).Distinct();

            var results = new List<StudentResultDto>();
            foreach (var studentId in studentIds)
            {
                var result = await CalculateStudentResultAsync(unitId, studentId);
                results.Add(result);
            }

            return results;
        }

        public async Task RecalculateResultsAsync(Guid unitId)
        {
            _logger.LogInformation("Recalculating all results for unit {UnitId}", unitId);

            // Get all students with marks in this unit
            var marks = await _markRepository.GetByUnitAndStudentAsync(unitId, Guid.Empty);
            var studentIds = marks.Select(m => m.StudentId).Distinct();

            foreach (var studentId in studentIds)
            {
                var result = await CalculateStudentResultAsync(unitId, studentId);

                // Update or create grade
                var existingGrades = await _gradeRepository.GetStudentGradesAsync(studentId);
                var existingGrade = existingGrades.FirstOrDefault(g => g.UnitId == unitId);

                if (existingGrade != null)
                {
                    existingGrade.Score = result.FinalScore;
                    existingGrade.LetterGrade = result.FinalGrade;
                    existingGrade.Remarks = result.GradeDescription;
                    await _gradeRepository.UpdateAsync(existingGrade);
                }
                else
                {
                    var grade = new Grade
                    {
                        StudentId = studentId,
                        UnitId = unitId,
                        Score = result.FinalScore,
                        LetterGrade = result.FinalGrade,
                        Remarks = result.GradeDescription,
                        GradedDate = DateTime.UtcNow,
                        IsPublished = false
                    };
                    await _gradeRepository.AddAsync(grade);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recalculation complete for unit {UnitId}", unitId);
        }

        #endregion

        #region Grading

        public async Task AssignGradesAsync(Guid unitId)
        {
            _logger.LogInformation("Assigning grades for unit {UnitId}", unitId);

            var results = await CalculateAllResultsAsync(unitId);

            foreach (var result in results)
            {
                var existingGrades = await _gradeRepository.GetStudentGradesAsync(result.StudentId);
                var grade = existingGrades.FirstOrDefault(g => g.UnitId == unitId);

                if (grade != null)
                {
                    grade.Score = result.FinalScore;
                    grade.LetterGrade = result.FinalGrade;
                    grade.Remarks = result.GradeDescription;
                    await _gradeRepository.UpdateAsync(grade);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Grade", "Assigned", unitId.ToString(),
                $"Assigned grades for unit {unitId}");
        }

        public async Task<StudentResultDto> GetStudentResultAsync(Guid unitId, Guid studentId)
        {
            return await CalculateStudentResultAsync(unitId, studentId);
        }

        #endregion

        #region Publication Workflow

        public async Task SubmitForReviewAsync(SubmitForReviewCommand command)
        {
            _logger.LogInformation("Submitting results for review: Unit {UnitId}", command.UnitId);

            // Validate all weights
            var weightValidation = await ValidateWeightsAsync(command.UnitId);
            if (!weightValidation.IsValid)
            {
                throw new InvalidOperationException("Cannot submit for review: Weight validation failed.");
            }

            // Update all assessments to Pending Review
            var assessments = await _assessmentRepository.GetByUnitAsync(command.UnitId);
            foreach (var assessment in assessments)
            {
                assessment.PublicationStatus = ResultPublicationStatus.PendingReview;
                await _assessmentRepository.UpdateAsync(assessment);
            }

            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Results", "SubmittedForReview", command.UnitId.ToString(),
                $"Submitted results for review for unit {command.UnitId}. Comments: {command.Comments}");
        }

        public async Task ApproveResultsAsync(ApproveResultsCommand command)
        {
            _logger.LogInformation("Approving results: Unit {UnitId}", command.UnitId);

            var assessments = await _assessmentRepository.GetByUnitAsync(command.UnitId);
            foreach (var assessment in assessments)
            {
                if (assessment.PublicationStatus != ResultPublicationStatus.PendingReview)
                {
                    throw new InvalidOperationException("Results must be in Pending Review status before approval.");
                }

                assessment.PublicationStatus = ResultPublicationStatus.Approved;
                await _assessmentRepository.UpdateAsync(assessment);
            }

            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Results", "Approved", command.UnitId.ToString(),
                $"Approved results for unit {command.UnitId}. Comments: {command.Comments}");
        }

        public async Task PublishResultsAsync(PublishResultsCommand command)
        {
            _logger.LogInformation("Publishing results: Unit {UnitId}", command.UnitId);

            var assessments = await _assessmentRepository.GetByUnitAsync(command.UnitId);
            foreach (var assessment in assessments)
            {
                if (assessment.PublicationStatus != ResultPublicationStatus.Approved)
                {
                    throw new InvalidOperationException("Results must be Approved before publication.");
                }

                assessment.PublicationStatus = ResultPublicationStatus.Published;
                assessment.Status = AssessmentStatus.Published;
                await _assessmentRepository.UpdateAsync(assessment);
            }

            await _unitOfWork.SaveChangesAsync();

            // Recalculate and update grades
            await RecalculateResultsAsync(command.UnitId);

            // Check certificate eligibility for all students
            var marks = await _markRepository.GetByUnitAndStudentAsync(command.UnitId, Guid.Empty);
            var studentIds = marks.Select(m => m.StudentId).Distinct();
            foreach (var studentId in studentIds)
            {
                await RecalculateCertificateEligibilityAsync(studentId);
            }

            // Audit log
            await _auditService.LogActivityAsync("Results", "Published", null,
                $"Published results for unit {command.UnitId}. Comments: {command.Comments}");
        }

        #endregion

        #region Moderation

        public async Task<StudentAssessmentMarkDto> ModerateMarkAsync(Guid markId, ModerationStatus status, string? comment)
        {
            _logger.LogInformation("Moderating mark {MarkId} with status {Status}", markId, status);

            var mark = await _markRepository.GetByIdAsync(markId);
            if (mark == null)
            {
                throw new KeyNotFoundException("Mark not found.");
            }

            mark.IsModerated = true;
            mark.ModeratedDate = DateTime.UtcNow;
            mark.ModeratedBy = _currentUserService.UserId?.ToString();
            mark.ModerationComment = comment;

            // TODO: Update assessment moderation status

            await _markRepository.UpdateAsync(mark);
            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Mark", "Moderated", markId.ToString(),
                $"Mark moderated with status {status}. Comment: {comment}");

            return new StudentAssessmentMarkDto
            {
                Id = mark.Id,
                AssessmentId = mark.AssessmentId,
                StudentId = mark.StudentId,
                Score = mark.Mark,
                IsDraft = mark.IsDraft,
                Feedback = mark.Feedback,
                PublicationStatus = mark.Assessment.PublicationStatus,
                ModerationStatus = mark.Assessment.ModerationStatus
            };
        }

        #endregion

        #region Grade Changes

        public async Task ChangeMarkAsync(ChangeMarkCommand command)
        {
            _logger.LogInformation("Changing mark {MarkId} to {NewScore}", command.MarkId, command.NewScore);

            var mark = await _markRepository.GetByIdAsync(command.MarkId);
            if (mark == null)
            {
                throw new KeyNotFoundException("Mark not found.");
            }

            var assessment = await _assessmentRepository.GetByIdAsync(mark.AssessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            // Store original values
            var originalMark = mark.Mark;
            var originalPercentage = mark.Percentage;
            var originalWeightedScore = mark.WeightedScore;

            // Validate new mark
            if (command.NewScore < 0 || command.NewScore > command.NewMaxScore)
            {
                throw new InvalidOperationException($"New score must be between 0 and {command.NewMaxScore}.");
            }

            // Update mark
            mark.Mark = command.NewScore;
            mark.RevisedMark = command.NewScore;
            await _markRepository.UpdateAsync(mark);

            // Recalculate results
            await RecalculateResultsAsync(assessment.UnitId);

            // Get updated result
            var result = await CalculateStudentResultAsync(assessment.UnitId, mark.StudentId);

            // Record grade change history
            var changeHistory = new GradeChangeHistory
            {
                AssessmentId = assessment.Id,
                StudentAssessmentMarkId = mark.Id,
                StudentId = mark.StudentId,
                UnitId = assessment.UnitId,
                PreviousScore = originalMark,
                NewScore = command.NewScore,
                PreviousGradeLetter = result.FinalGrade, // TODO: Get previous grade
                NewGradeLetter = result.FinalGrade,
                ChangeReason = GradeChangeReason.Correction,
                Reason = command.Reason,
                ChangedBy = _currentUserService.UserId?.ToString(),
                ChangedDate = DateTime.UtcNow,
                ChangeDetailsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    PreviousPercentage = originalPercentage,
                    NewPercentage = mark.Percentage,
                    PreviousWeightedScore = originalWeightedScore,
                    NewWeightedScore = mark.WeightedScore
                })
            };

            // TODO: Save change history to repository

            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Mark", "Changed", mark.Id.ToString(),
                $"Mark changed from {originalMark} to {command.NewScore} for student {mark.StudentId}. Reason: {command.Reason}");
        }

        #endregion

        #region Locking

        public async Task LockUnitAsync(LockUnitCommand command)
        {
            _logger.LogInformation("Locking unit: {UnitId}", command.UnitId);

            var assessments = await _assessmentRepository.GetByUnitAsync(command.UnitId);
            foreach (var assessment in assessments)
            {
                assessment.IsWeightLocked = true;
                assessment.WeightLockedDate = DateTime.UtcNow;
                assessment.WeightLockedBy = _currentUserService.UserId?.ToString();
                await _assessmentRepository.UpdateAsync(assessment);
            }

            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Unit", "Locked", command.UnitId.ToString(),
                $"Unit {command.UnitId} locked. Reason: {command.Reason}");
        }

        public async Task UnlockUnitAsync(UnlockUnitCommand command)
        {
            _logger.LogInformation("Unlocking unit: {UnitId}", command.UnitId);

            var assessments = await _assessmentRepository.GetByUnitAsync(command.UnitId);
            foreach (var assessment in assessments)
            {
                assessment.IsWeightLocked = false;
                assessment.WeightLockedDate = null;
                assessment.WeightLockedBy = null;
                await _assessmentRepository.UpdateAsync(assessment);
            }

            await _unitOfWork.SaveChangesAsync();

            // Audit log
            await _auditService.LogActivityAsync("Unit", "Unlocked", command.UnitId.ToString(),
                $"Unit {command.UnitId} unlocked. Reason: {command.Reason}");
        }

        #endregion

        #region Certificate Eligibility

        public async Task<bool> CheckCertificateEligibilityAsync(Guid studentId, Guid unitId)
        {
            _logger.LogInformation("Checking certificate eligibility for student {StudentId} in unit {UnitId}",
                studentId, unitId);

            // NOTE: This method must NOT call CalculateStudentResultAsync because
            // CalculateStudentResultAsync calls this method (line ~464), which would
            // cause infinite recursion and a stack overflow. Instead, compute the
            // final score and passing status directly from the repositories.

            // Check all conditions
            // 1. All mandatory assessments completed
            var assessments = await _assessmentRepository.GetByUnitAsync(unitId);
            var mandatoryAssessments = assessments.Where(a => a.IsMandatory).ToList();
            var marks = await _markRepository.GetByUnitAndStudentAsync(unitId, studentId);

            foreach (var assessment in mandatoryAssessments)
            {
                var mark = marks.FirstOrDefault(m => m.AssessmentId == assessment.Id);
                if (mark == null || mark.IsDraft)
                {
                    return false; // Missing mandatory assessment
                }
            }

            // 2. No incomplete assessments
            if (marks.Any(m => m.IsDraft))
            {
                return false;
            }

            // 3. Compute final score directly (same logic as CalculateStudentResultAsync)
            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;

            foreach (var assessment in assessments.Where(a => a.IsActive))
            {
                var mark = marks.FirstOrDefault(m => m.AssessmentId == assessment.Id);

                if (mark != null && !mark.IsExempt)
                {
                    var percentage = (mark.Mark / assessment.MaxScore) * 100;
                    var weightedScore = percentage * (assessment.Weight / 100);
                    totalWeightedScore += weightedScore;
                    totalWeight += assessment.Weight;
                }
                else if (mark != null && mark.IsExempt)
                {
                    // Exempt - skip entirely (matches CalculateStudentResultAsync).
                    continue;
                }
            }

            // Normalize the weighted score to the total non-exempt weight
            // (matches CalculateStudentResultAsync). E.g. a 90% on the only
            // remaining 70% assessment yields 90.00, not 63.00.
            var finalScore = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : 0;

            // 4. Passing grade (final score >= 50)
            if (finalScore < 50)
            {
                return false;
            }

            // TODO: Check additional institutional requirements

            return true;
        }

        public async Task RecalculateCertificateEligibilityAsync(Guid studentId)
        {
            _logger.LogInformation("Recalculating certificate eligibility for student {StudentId}", studentId);

            // Get all units for student
            // TODO: Get from enrollment repository
            var unitIds = new List<Guid>(); // Placeholder

            foreach (var unitId in unitIds)
            {
                var isEligible = await CheckCertificateEligibilityAsync(studentId, unitId);

                // Update certificate eligibility
                // TODO: Update in certificate system
            }
        }

        #endregion

        #region IAssessmentEngine Interface Implementation

        public async Task<bool> ValidateWeightTotalAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var result = await ValidateWeightsAsync(unitId);
            return result.IsValid;
        }

        public async Task<decimal> GetTotalWeightAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var assessments = await _assessmentRepository.GetByUnitAsync(unitId);
            return assessments.Sum(a => a.Weight);
        }

        public async Task<decimal> CalculateWeightedScoreAsync(decimal mark, decimal maxScore, decimal weight)
        {
            var percentage = (mark / maxScore) * 100;
            return percentage * (weight / 100);
        }

        public async Task<StudentAssessmentMark> CalculateAndSaveMarkAsync(Guid assessmentId, Guid studentId, decimal mark, CancellationToken ct = default)
        {
            var assessment = await _assessmentRepository.GetByIdAsync(assessmentId);
            if (assessment == null)
            {
                throw new KeyNotFoundException("Assessment not found.");
            }

            var percentage = (mark / assessment.MaxScore) * 100;
            var weightedScore = percentage * (assessment.Weight / 100);

            // TODO: Create or update StudentAssessmentMark entity
            // For now, return a placeholder
            return new StudentAssessmentMark
            {
                AssessmentId = assessmentId,
                StudentId = studentId,
                Mark = mark,
                Percentage = percentage,
                WeightedScore = weightedScore
            };
        }

        public async Task<UnitResult> CalculateFinalUnitScoreAsync(Guid studentId, Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var resultDto = await CalculateStudentResultAsync(unitId, studentId);
            return new UnitResult
            {
                StudentId = studentId,
                UnitId = unitId,
                FinalPercentage = resultDto.FinalScore,
                GradeLetter = resultDto.FinalGrade,
                GradeDescription = resultDto.GradeDescription
            };
        }

        public async Task<decimal> CalculateFinalPercentageAsync(IEnumerable<StudentAssessmentMark> marks, IEnumerable<Assessment> assessments)
        {
            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;

            foreach (var assessment in assessments.Where(a => a.IsActive))
            {
                var mark = marks.FirstOrDefault(m => m.AssessmentId == assessment.Id);
                if (mark != null && !mark.IsExempt)
                {
                    var percentage = (mark.Mark / assessment.MaxScore) * 100;
                    var weightedScore = percentage * (assessment.Weight / 100);
                    totalWeightedScore += weightedScore;
                    totalWeight += assessment.Weight;
                }
            }

            return totalWeight > 0 ? totalWeightedScore : 0;
        }

        public async Task<(string GradeLetter, string Description, decimal? GpaPoints)> AssignGradeAsync(decimal percentage, CancellationToken ct = default)
        {
            var gradingScale = await _gradingScaleRepository.GetActiveVersionAsync();
            if (gradingScale == null)
            {
                return ("F", "Fail", null);
            }

            var gradeBands = await _gradeBandRepository.GetByScaleAsync(gradingScale.Id);
            var gradeBand = gradeBands.FirstOrDefault(b => percentage >= b.MinPercentage && percentage <= b.MaxPercentage);

            if (gradeBand == null)
            {
                return ("F", "Fail", null);
            }

            return (gradeBand.GradeLetter, gradeBand.Description, gradeBand.GpaPoints);
        }

        public async Task<GradeBand?> FindGradeBandAsync(decimal percentage, CancellationToken ct = default)
        {
            var gradingScale = await _gradingScaleRepository.GetActiveVersionAsync();
            if (gradingScale == null)
            {
                return null;
            }

            var gradeBands = await _gradeBandRepository.GetByScaleAsync(gradingScale.Id);
            return gradeBands.FirstOrDefault(b => percentage >= b.MinPercentage && percentage <= b.MaxPercentage);
        }

        public async Task<StudentCertificateEligibility> EvaluateCertificateEligibilityAsync(Guid studentId, CancellationToken ct = default)
        {
            // TODO: Implement comprehensive certificate eligibility check
            return new StudentCertificateEligibility
            {
                StudentId = studentId,
                Status = CertificateEligibilityStatus.NotDetermined,
                EligibilityDetails = "Not implemented yet"
            };
        }

        public async Task PublishUnitResultsAsync(Guid unitId, Guid? courseOfferingId, string publishedBy, CancellationToken ct = default)
        {
            var command = new PublishResultsCommand
            {
                UnitId = unitId,
                CourseOfferingId = courseOfferingId,
                Comments = $"Published by {publishedBy}"
            };
            await PublishResultsAsync(command);
        }

        public async Task ApproveUnitResultsAsync(Guid unitId, Guid? courseOfferingId, string approvedBy, CancellationToken ct = default)
        {
            var command = new ApproveResultsCommand
            {
                UnitId = unitId,
                CourseOfferingId = courseOfferingId,
                Comments = $"Approved by {approvedBy}"
            };
            await ApproveResultsAsync(command);
        }

        public async Task RecalculateAfterGradeChangeAsync(Guid studentId, Guid unitId, CancellationToken ct = default)
        {
            await RecalculateResultsAsync(unitId);
        }

        public async Task<IEnumerable<UnitResult>> CalculateAllUnitResultsAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var results = await CalculateAllResultsAsync(unitId);
            return results.Select(r => new UnitResult
            {
                StudentId = r.StudentId,
                UnitId = r.UnitId,
                FinalPercentage = r.FinalScore,
                GradeLetter = r.FinalGrade,
                GradeDescription = r.GradeDescription
            });
        }

        #endregion
    }
}
