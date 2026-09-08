using MediatR;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Lecturer dashboard: assessments, statistics, pass rate, grade distribution,
    /// at-risk students, incomplete assessments and pending grading tasks.
    /// Grade letters are assigned by the centralized engine; no independent
    /// grading math occurs here.
    /// </summary>
    public class GetLecturerDashboardHandler : IRequestHandler<GetLecturerDashboardQuery, LecturerDashboardDto>
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IAssessmentEngine _engine;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ICertificateRuleRepository _certificateRuleRepository;

        public GetLecturerDashboardHandler(
            IAssessmentRepository assessmentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentEngine engine,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ICertificateRuleRepository certificateRuleRepository)
        {
            _assessmentRepository = assessmentRepository;
            _markRepository = markRepository;
            _engine = engine;
            _currentUser = currentUser;
            _certificateRuleRepository = certificateRuleRepository;
        }

        public async Task<LecturerDashboardDto> Handle(GetLecturerDashboardQuery request, CancellationToken cancellationToken)
        {
            var assessments = (await _assessmentRepository.GetByLecturerAsync(request.LecturerId, cancellationToken))
                .Where(a => !a.IsDeleted)
                .ToList();

            var rule = await _certificateRuleRepository.GetActiveRuleAsync(cancellationToken);
            var minPass = rule?.MinimumPassingPercentage ?? 50m;

            var dto = new LecturerDashboardDto
            {
                LecturerId = request.LecturerId,
                LecturerName = _currentUser.Username,
                Assessments = new(),
                StudentResults = new(),
                PassRate = 0m,
                GradeDistribution = new(),
                StudentsAtRisk = new(),
                IncompleteAssessments = new(),
                PendingGradingTasks = new(),
                TotalAssessments = assessments.Count,
                TotalStudents = 0
            };

            var atRisk = new List<StudentResultDto>();
            var letters = new List<string>();
            var totalPass = 0;
            var totalMarks = 0;
            var studentIds = new HashSet<Guid>();

            foreach (var assessment in assessments)
            {
                dto.Assessments.Add(GetAssessmentHandler.Map(assessment));

                var marks = (await _markRepository.GetByAssessmentAsync(assessment.Id, cancellationToken))
                    .Where(m => !m.IsDraft)
                    .ToList();

                var pending = await _markRepository.CountPendingGradingAsync(assessment.Id, cancellationToken);
                if (pending > 0 || marks.Count == 0)
                {
                    dto.IncompleteAssessments.Add(GetAssessmentHandler.Map(assessment));
                    dto.PendingGradingTasks.Add(GetAssessmentHandler.Map(assessment));
                }

                foreach (var mark in marks)
                {
                    studentIds.Add(mark.StudentId);
                    var percentage = mark.Percentage > 0m ? mark.Percentage : (mark.Mark / assessment.MaxScore) * 100m;
                    var grade = (await _engine.AssignGradeAsync(percentage, cancellationToken)).GradeLetter;

                    letters.Add(grade);
                    totalMarks++;
                    if (percentage >= minPass)
                        totalPass++;
                    else
                    {
                        atRisk.Add(new StudentResultDto
                        {
                            StudentId = mark.StudentId,
                            UnitId = assessment.UnitId,
                            FinalScore = percentage,
                            FinalGrade = grade,
                            PublicationStatus = SMS.Domain.Enums.ResultPublicationStatus.Draft
                        });
                    }
                }
            }

            dto.GradeDistribution = letters.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            dto.PassRate = totalMarks > 0 ? Math.Round((totalPass * 100m) / totalMarks, 2) : 0m;
            dto.TotalStudents = studentIds.Count;
            dto.StudentsAtRisk = atRisk;

            return dto;
        }
    }
}