using MediatR;
using SMS.Application.Common.Interfaces;

namespace SMS.Application.Features.Auth.Queries
{
    /// <summary>
    /// Checks whether a candidate username is available (not already in use
    /// and well-formed). Used by the registration UI for real-time validation.
    /// </summary>
    public class CheckUsernameAvailabilityQuery : IRequest<UsernameAvailabilityDto>
    {
        public string Username { get; set; } = string.Empty;
    }

    public class UsernameAvailabilityDto
    {
        public bool IsAvailable { get; set; }
        public string? SuggestedUsername { get; set; }
        public string? Message { get; set; }
    }

    public class CheckUsernameAvailabilityQueryHandler : IRequestHandler<CheckUsernameAvailabilityQuery, UsernameAvailabilityDto>
    {
        private readonly IUsernameGenerator _usernameGenerator;

        public CheckUsernameAvailabilityQueryHandler(IUsernameGenerator usernameGenerator)
        {
            _usernameGenerator = usernameGenerator;
        }

        public async Task<UsernameAvailabilityDto> Handle(CheckUsernameAvailabilityQuery request, CancellationToken cancellationToken)
        {
            var isAvailable = await _usernameGenerator.IsUsernameAvailableAsync(request.Username);

            return new UsernameAvailabilityDto
            {
                IsAvailable = isAvailable,
                Message = isAvailable
                    ? "Username is available"
                    : "Username is already taken or invalid"
            };
        }
    }
}
