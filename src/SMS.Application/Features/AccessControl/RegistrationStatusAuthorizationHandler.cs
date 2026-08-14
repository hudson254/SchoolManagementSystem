using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.AccessControl
{
    /// <summary>
    /// Authorization requirement that checks if the current user's registration
    /// status is Approved before allowing access to restricted resources.
    /// </summary>
    public class RegistrationApprovedRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Authorization handler that verifies the user has Approved registration status.
    /// </summary>
    public class RegistrationStatusAuthorizationHandler
        : AuthorizationHandler<RegistrationApprovedRequirement>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;

        public RegistrationStatusAuthorizationHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository)
        {
            _currentUserService = currentUserService;
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RegistrationApprovedRequirement requirement)
        {
            var email = _currentUserService.Email;
            if (string.IsNullOrEmpty(email))
            {
                context.Fail();
                return;
            }

            // Check if user is a student with approved status
            var student = await _studentRepository.GetStudentByEmailAsync(email);
            if (student != null && student.RegistrationStatus == RegistrationStatus.Approved)
            {
                context.Succeed(requirement);
                return;
            }

            // Check if user is a lecturer with approved status
            var lecturer = await _lecturerRepository.GetLecturerByEmailAsync(email);
            if (lecturer != null && lecturer.RegistrationStatus == RegistrationStatus.Approved)
            {
                context.Succeed(requirement);
                return;
            }

            // User is not approved
            context.Fail();
        }
    }
}
