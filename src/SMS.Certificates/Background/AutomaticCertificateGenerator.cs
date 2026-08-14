using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SMS.Certificates.Application.Services;
using SMS.Domain.Interfaces;

namespace SMS.Certificates.Background;

/// <summary>
/// Background service that automatically generates certificates for completed course offerings
/// </summary>
public class AutomaticCertificateGenerator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomaticCertificateGenerator> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public AutomaticCertificateGenerator(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomaticCertificateGenerator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Automatic certificate generator started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCompletedOfferingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic certificate generation cycle");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Normal shutdown
                break;
            }
        }

        _logger.LogInformation("Automatic certificate generator stopped");
    }

    private async Task ProcessCompletedOfferingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var courseOfferingRepository = scope.ServiceProvider.GetRequiredService<ICourseOfferingRepository>();
        var bulkCertificateService = scope.ServiceProvider.GetRequiredService<BulkCertificateService>();

        // Find completed course offerings
        var completedOfferings = await courseOfferingRepository.GetCompletedOfferingsAsync(cancellationToken);

        foreach (var offering in completedOfferings)
        {
            try
            {
                _logger.LogInformation("Auto-generating certificates for completed offering {OfferingCode}", offering.OfferingCode);

                var result = await bulkCertificateService.GenerateForCourseOfferingAsync(
                    offering.Id,
                    userId: null,
                    userRole: "System",
                    ipAddress: null,
                    sessionId: null,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Auto-generation for offering {OfferingCode}: {GeneratedCount} generated, {SkippedCount} skipped, {ErrorCount} errors",
                    offering.OfferingCode,
                    result.Generated.Count,
                    result.Skipped.Count,
                    result.Errors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-generating certificates for offering {OfferingCode}", offering.OfferingCode);
            }
        }
    }
}
