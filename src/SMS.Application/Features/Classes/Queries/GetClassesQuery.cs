using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Classes.Commands;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Classes.Queries
{
    public class GetClassesQuery : IRequest<IEnumerable<ClassDto>>
    {
        public string? SearchTerm { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? UnitId { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class GetClassQuery : IRequest<ClassDto>
    {
        public Guid ClassId { get; set; }
    }

    public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, IEnumerable<ClassDto>>
    {
        private readonly IClassRepository _classRepository;
        private readonly ILogger<GetClassesQueryHandler> _logger;

        public GetClassesQueryHandler(IClassRepository classRepository, ILogger<GetClassesQueryHandler> logger)
        {
            _classRepository = classRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<ClassDto>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            var classes = await _classRepository.GetAllWithDetailsAsync(cancellationToken);

            var query = classes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(c =>
                    c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.Unit != null && c.Unit.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }
            if (request.SemesterId.HasValue)
                query = query.Where(c => c.SemesterId == request.SemesterId.Value);
            if (request.UnitId.HasValue)
                query = query.Where(c => c.UnitId == request.UnitId.Value);
            if (!request.IncludeInactive)
                query = query.Where(c => c.IsActive);

            return query
                .OrderBy(c => c.Name)
                .Select(c => ClassDtoMapper.ToDto(c, c.Unit, c.Lecturer, c.Semester))
                .ToList();
        }
    }

    public class GetClassQueryHandler : IRequestHandler<GetClassQuery, ClassDto>
    {
        private readonly IClassRepository _classRepository;
        private readonly ILogger<GetClassQueryHandler> _logger;

        public GetClassQueryHandler(IClassRepository classRepository, ILogger<GetClassQueryHandler> logger)
        {
            _classRepository = classRepository;
            _logger = logger;
        }

        public async Task<ClassDto> Handle(GetClassQuery request, CancellationToken cancellationToken)
        {
            var klass = await _classRepository.GetByIdAsync(request.ClassId, cancellationToken);
            if (klass == null)
                throw new NotFoundException("Class", request.ClassId);

            var details = (await _classRepository.GetAllWithDetailsAsync(cancellationToken))
                .FirstOrDefault(c => c.Id == request.ClassId);

            return ClassDtoMapper.ToDto(details ?? klass, details?.Unit, details?.Lecturer, details?.Semester);
        }
    }
}