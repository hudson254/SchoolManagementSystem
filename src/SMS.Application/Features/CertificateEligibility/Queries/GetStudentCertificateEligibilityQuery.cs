using MediatR;
using SMS.Application.Features.CertificateEligibility.DTOs;

namespace SMS.Application.Features.CertificateEligibility.Queries
{
    public class GetStudentCertificateEligibilityQuery : IRequest<StudentCertificateEligibilityDto>
    {
        public Guid StudentId { get; set; }
    }
}

