using Microsoft.Extensions.Logging;
using SMS.Certificates.Domain.Interfaces;

namespace SMS.Certificates.Infrastructure.Services;

/// <summary>
/// Implementation of certificate number generator
/// </summary>
public class CertificateNumberGenerator : ICertificateNumberGenerator
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ILogger<CertificateNumberGenerator> _logger;

    /// <summary>
    /// Format: SMS-{YEAR}-{COURSE_CODE}-{SEQUENTIAL}
    /// Example: SMS-2026-DIT-000001
    /// </summary>
    private const string CERTIFICATE_NUMBER_FORMAT = "SMS-{0}-{1}-{2:D6}";

    public CertificateNumberGenerator(
        ICertificateRepository certificateRepository,
        ILogger<CertificateNumberGenerator> logger)
    {
        _certificateRepository = certificateRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateCertificateNumberAsync(string courseCode, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            var sequentialNumber = await GetNextSequentialNumberAsync(courseCode, year, cancellationToken);
            return string.Format(CERTIFICATE_NUMBER_FORMAT, year, courseCode.ToUpper(), sequentialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate number for course {CourseCode} year {Year}", courseCode, year);
            throw;
        }
    }

    /// <inheritdoc/>
    public bool ValidateCertificateNumber(string certificateNumber)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            return false;

        // Expected format: SMS-2026-DIT-000001
        var parts = certificateNumber.Split('-');
        if (parts.Length != 4)
            return false;

        if (parts[0] != "SMS")
            return false;

        if (!int.TryParse(parts[1], out int year))
            return false;

        if (string.IsNullOrWhiteSpace(parts[2]))
            return false;

        if (!int.TryParse(parts[3], out int sequential))
            return false;

        return year > 2000 && sequential > 0;
    }

    /// <inheritdoc/>
    public async Task<int> GetNextSequentialNumberAsync(string courseCode, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all certificates for this course code and year
            var prefix = $"SMS-{year}-{courseCode.ToUpper()}-";
            var allCertificates = await _certificateRepository.GetByCourseOfferingIdAsync(
                Guid.Empty, // We'll filter by certificate number pattern instead
                cancellationToken);

            // Filter certificates matching the pattern for this year and course
            var matchingCertificates = new List<string>();
            foreach (var cert in allCertificates)
            {
                if (cert.CertificateNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matchingCertificates.Add(cert.CertificateNumber);
                }
            }

            // Also check if any certificate with this pattern exists
            // Since we don't have a direct query, we'll use a different approach
            // Query all certificates and filter in memory (not ideal but works)
            var allCerts = await _certificateRepository.GetActiveCertificatesAsync(cancellationToken);
            var maxSequential = 0;

            foreach (var cert in allCerts)
            {
                if (cert.CertificateNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var parts = cert.CertificateNumber.Split('-');
                    if (parts.Length == 4 && int.TryParse(parts[3], out int sequential))
                    {
                        if (sequential > maxSequential)
                            maxSequential = sequential;
                    }
                }
            }

            return maxSequential + 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next sequential number for course {CourseCode} year {Year}", courseCode, year);
            throw;
        }
    }
}
