namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Service for generating unique certificate numbers
/// </summary>
public interface ICertificateNumberGenerator
{
    /// <summary>
    /// Generate a unique certificate number
    /// </summary>
    /// <param name="courseCode">Course code (e.g., DIT)</param>
    /// <param name="year">Award year</param>
    /// <returns>Unique certificate number</returns>
    Task<string> GenerateCertificateNumberAsync(string courseCode, int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a certificate number format
    /// </summary>
    bool ValidateCertificateNumber(string certificateNumber);

    /// <summary>
    /// Get the next sequential number for a given course code and year
    /// </summary>
    Task<int> GetNextSequentialNumberAsync(string courseCode, int year, CancellationToken cancellationToken = default);
}
