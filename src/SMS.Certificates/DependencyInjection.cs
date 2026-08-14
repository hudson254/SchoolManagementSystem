using Microsoft.Extensions.DependencyInjection;
using SMS.Certificates.Domain.Interfaces;
using SMS.Certificates.Infrastructure.Services;
using SMS.Certificates.Application.Services;
using SMS.Certificates.Background;

namespace SMS.Certificates;

/// <summary>
/// Dependency injection registration for Certificate module
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register certificate services
    /// </summary>
    public static IServiceCollection AddCertificateModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICertificateNumberGenerator, CertificateNumberGenerator>();
        services.AddScoped<ICertificateEligibilityService, CertificateEligibilityService>();
        services.AddScoped<ICertificateVerificationService, CertificateVerificationService>();
        services.AddScoped<ICertificatePdfGenerator, CertificatePdfGenerator>();

        // Register application services
        services.AddScoped<CertificateService>();
        services.AddScoped<BulkCertificateService>();

        // Register background service for automatic certificate generation
        services.AddHostedService<AutomaticCertificateGenerator>();

        return services;
    }
}
