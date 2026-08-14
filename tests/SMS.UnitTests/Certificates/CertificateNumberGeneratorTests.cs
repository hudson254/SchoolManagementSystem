using Microsoft.Extensions.Logging;
using Moq;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Certificates.Infrastructure.Services;
using Xunit;

namespace SMS.UnitTests.Certificates;

public class CertificateNumberGeneratorTests
{
    private readonly Mock<ICertificateRepository> _repo;
    private readonly Mock<ILogger<CertificateNumberGenerator>> _logger;
    private readonly CertificateNumberGenerator _generator;

    public CertificateNumberGeneratorTests()
    {
        _repo = new Mock<ICertificateRepository>();
        _logger = new Mock<ILogger<CertificateNumberGenerator>>();
        _generator = new CertificateNumberGenerator(_repo.Object, _logger.Object);
    }

    [Fact]
    public async Task GenerateCertificateNumber_ReturnsCorrectFormat()
    {
        // Arrange
        _repo.Setup(r => r.GetActiveCertificatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Certificate>());

        // Act
        var result = await _generator.GenerateCertificateNumberAsync("DIT", 2026);

        // Assert
        Assert.Equal("SMS-2026-DIT-000001", result);
    }

    [Fact]
    public async Task GenerateCertificateNumber_IncrementsSequential()
    {
        // Arrange
        var existing = new List<Certificate>
        {
            new() { CertificateNumber = "SMS-2026-DIT-000001", Status = CertificateStatus.Issued },
            new() { CertificateNumber = "SMS-2026-DIT-000002", Status = CertificateStatus.Issued },
            new() { CertificateNumber = "SMS-2026-DIT-000003", Status = CertificateStatus.Revoked }
        };
        _repo.Setup(r => r.GetActiveCertificatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _generator.GenerateCertificateNumberAsync("DIT", 2026);

        // Assert
        Assert.Equal("SMS-2026-DIT-000004", result);
    }

    [Fact]
    public async Task GenerateCertificateNumber_IsCourseCodeSpecific()
    {
        // Arrange
        var existing = new List<Certificate>
        {
            new() { CertificateNumber = "SMS-2026-DIT-000001", Status = CertificateStatus.Issued },
            new() { CertificateNumber = "SMS-2026-BBA-000001", Status = CertificateStatus.Issued }
        };
        _repo.Setup(r => r.GetActiveCertificatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _generator.GenerateCertificateNumberAsync("BBA", 2026);

        // Assert
        Assert.Equal("SMS-2026-BBA-000002", result);
    }

    [Fact]
    public async Task GenerateCertificateNumber_IsYearSpecific()
    {
        // Arrange
        var existing = new List<Certificate>
        {
            new() { CertificateNumber = "SMS-2025-DIT-000001", Status = CertificateStatus.Issued },
            new() { CertificateNumber = "SMS-2026-DIT-000001", Status = CertificateStatus.Issued }
        };
        _repo.Setup(r => r.GetActiveCertificatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _generator.GenerateCertificateNumberAsync("DIT", 2026);

        // Assert
        Assert.Equal("SMS-2026-DIT-000002", result);
    }

    [Theory]
    [InlineData("SMS-2026-DIT-000001", true)]
    [InlineData("SMS-2026-DIT-000999", true)]
    [InlineData("SMS-2026-DIT-000000", false)]
    [InlineData("SMS-2026-DIT", false)]
    [InlineData("SMS-2026-DIT-000001-EXTRA", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("SMS-1999-DIT-000001", false)]
    [InlineData("SMS-2026-ABC-000001", true)]
    public void ValidateCertificateNumber_ReturnsExpected(string? number, bool expected)
    {
        // Act
        var result = _generator.ValidateCertificateNumber(number!);

        // Assert
        Assert.Equal(expected, result);
    }
}
