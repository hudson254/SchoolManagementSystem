using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.StudyMaterials.Queries
{
    public class GetUnitStudyMaterialsQuery : IRequest<IEnumerable<StudyMaterialDto>>
    {
        public Guid UnitId { get; set; }
    }

    public class GetUnitStudyMaterialsQueryValidator : AbstractValidator<GetUnitStudyMaterialsQuery>
    {
        public GetUnitStudyMaterialsQueryValidator()
        {
            RuleFor(x => x.UnitId).NotEmpty().WithMessage("Unit ID is required");
        }
    }

    public class GetUnitStudyMaterialsQueryHandler
        : IRequestHandler<GetUnitStudyMaterialsQuery, IEnumerable<StudyMaterialDto>>
    {
        private readonly ILectureNoteRepository _lectureNoteRepository;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<GetUnitStudyMaterialsQueryHandler> _logger;

        public GetUnitStudyMaterialsQueryHandler(
            ILectureNoteRepository lectureNoteRepository,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<GetUnitStudyMaterialsQueryHandler> logger)
        {
            _lectureNoteRepository = lectureNoteRepository;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<IEnumerable<StudyMaterialDto>> Handle(
            GetUnitStudyMaterialsQuery request, CancellationToken cancellationToken)
        {
            if (!await CanViewUnitMaterialsAsync(request.UnitId, cancellationToken))
            {
                throw new ForbiddenException("StudyMaterial", _currentUserService.UserId ?? "unknown");
            }

            var notes = await _lectureNoteRepository.GetByUnitAsync(request.UnitId, cancellationToken);
            var published = notes.Where(n => n.IsPublished).ToList();
            return published.Select(StudyMaterialMappings.ToDto).ToList();
        }

        private async Task<bool> CanViewUnitMaterialsAsync(Guid unitId, CancellationToken cancellationToken)
        {
            if (_academicAccessService.IsAdminOrCoordinator())
            {
                return true;
            }

            if (_academicAccessService.IsLecturerRole() &&
                await _academicAccessService.LecturerTeachesUnitAsync(unitId, cancellationToken))
            {
                return true;
            }

            if (_academicAccessService.IsStudentRole() &&
                await _academicAccessService.StudentEnrolledInUnitAsync(unitId, cancellationToken))
            {
                return true;
            }

            return false;
        }
    }
}