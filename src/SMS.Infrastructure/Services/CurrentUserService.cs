using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SMS.Application.Common.Interfaces;

namespace SMS.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string Username => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        public string Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
            ?.Select(c => c.Value) ?? new List<string>();
    }
}
