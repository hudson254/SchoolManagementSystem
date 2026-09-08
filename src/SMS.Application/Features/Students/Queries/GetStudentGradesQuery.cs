using Microsoft.Extensions.Logging;
using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Features.Grades;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentGradesQuery : IRequest<IEnumerable<GradeDto>>
    {
        public Guid StudentId { get; set; }
        public Guid? SemesterId { get; set; }
        public bool? IsPublished { get; set; }
    }

    /// <summary>
    /// Student-facing grades feed backed by the authoritative grading engine.
    /// Returns only PUBLISHED UnitResults (draft/pending/approved-but-unpublished
    /// results are never exposed to students) mapped through
    /// <see cref="AuthoritativeGradeMapper"/>, so no independent grade math exists
    /// on this path and legacy hard-coded grade points are not used.

    /// </summary>
    public class GetStudentGradesQueryHandler : IRequestHandler<GetStudentGradesQuery, IEnumerable<GradeDto>>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly ILogger<GetStudentGradesQueryHandler> _logger;

        public GetStudentGradesQueryHandler(
            IStudentRepository studentRepository,
            IUnitResultRepository unitResultRepository,
            ILogger<GetStudentGradesQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _unitResultRepository = unitResultRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<GradeDto>> Handle(GetStudentGradesQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            var results = (await _unitResultRepository.GetPublishedByStudentAsync(request.StudentId, cancellationToken))
                .Where(r => !r.IsDeleted)
.ToList();

            if (request.SemesterId.HasValue)
                results = results.Where(r => r.SemesterId == request.SemesterId.Value).ToList();

            _logger.LogInformation("Loaded {Count} published unit results for student {StudentId}", results.Count, request.StudentId);

            return results.Select(r => AuthoritativeGradeMapper.MapToGradeDto(r, null, student)).ToList();
        }
    }
}