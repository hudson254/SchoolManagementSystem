namespace SMS.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? GetUserId();
        string? GetUserEmail();
        string? GetUserRole();
        bool IsAuthenticated();
        bool IsInRole(string role);
        bool HasPermission(string permission);
        Guid GetTenantId();
    }
}