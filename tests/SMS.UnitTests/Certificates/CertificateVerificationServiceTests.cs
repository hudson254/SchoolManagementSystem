using Microsoft.Extensions.Logging;
using Moq;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Certificates.Infrastructure.Services;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Certificates;

public class CertificateVerificationServiceTests
{
    private readonly Mock<ICertificateRepository> _certRepo;
    private readonly Mock<ICertificateAuditLogRepository> _auditRepo;
    private readonly Mock<IStudentRepository> _studentRepo;
    private readonly Mock<ICourseOfferingRepository> _offeringRepo;
    private readonly Mock<ILogger<CertificateVerificationService>> _logger;
    private readonly CertificateVerificationService _service;

    public CertificateVerificationServiceTests()
    {
        _certRepo = new Mock<ICertificateRepository>();
        _auditRepo = new Mock<ICertificateAuditLogRepository>();
        _studentRepo = new Mock<IStudentRepository>();
        _offeringRepo = new Mock<ICourseOfferingRepository>();
        _logger = new Mock<ILogger<CertificateVerificationService>>();
        _service = new CertificateVerificationService(
            _certRepo.Object,
            _auditRepo.Object,
            _studentRepo.Object,
            _offeringRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task VerifyByCertificateNumber_ValidCertificate_ReturnsValidResult()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Issued,
            FinalGrade = "A",
            Classification = "Distinction",
            IssueDate = DateTime.UtcNow,
            VerificationToken = "token123"
        };
        _certRepo.Setup(r => r.GetByCertificateNumberAsync("SMS-2026-DIT-000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);
        _studentRepo.Setup(r => r.GetStudentWithDetailsAsync(certificate.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { FirstName = "John", LastName = "Doe" });
        _offeringRepo.Setup(r => r.GetWithDetailsAsync(certificate.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseOffering { OfferingCode = "DIT-2026-08", Course = new Course { Name = "Diploma in IT" } });

        // Act
        var result = await _service.VerifyByCertificateNumberAsync("SMS-2026-DIT-000001");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Certificate);
        Assert.Equal("SMS-2026-DIT-000001", result.Certificate.CertificateNumber);
        Assert.Equal("John Doe", result.Certificate.StudentName);
        Assert.Equal("Diploma in IT", result.Certificate.CourseName);
        Assert.Equal("DIT-2026-08", result.Certificate.CourseOffering);
        Assert.Equal("A", result.Certificate.FinalGrade);
        Assert.Equal("Distinction", result.Certificate.Classification);
        Assert.Equal("Issued", result.Certificate.Status);
    }

    [Fact]
    public async Task VerifyByCertificateNumber_CertificateNotFound_ReturnsInvalid()
    {
        // Arrange
        _certRepo.Setup(r => r.GetByCertificateNumberAsync("SMS-2026-DIT-999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Certificate?)null);

        // Act
        var result = await _service.VerifyByCertificateNumberAsync("SMS-2026-DIT-999999");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Certificate not found or invalid.", result.ErrorMessage);
        Assert.Null(result.Certificate);
    }

    [Fact]
    public async Task VerifyByCertificateNumber_RevokedCertificate_ReturnsInvalid()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Revoked,
            IssueDate = DateTime.UtcNow,
            VerificationToken = "token123"
        };
        _certRepo.Setup(r => r.GetByCertificateNumberAsync("SMS-2026-DIT-000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);

        // Act
        var result = await _service.VerifyByCertificateNumberAsync("SMS-2026-DIT-000001");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Certificate not found or invalid.", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyByCertificateNumber_ExpiredCertificate_ReturnsInvalid()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Issued,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
            VerificationToken = "token123"
        };
        _certRepo.Setup(r => r.GetByCertificateNumberAsync("SMS-2026-DIT-000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);

        // Act
        var result = await _service.VerifyByCertificateNumberAsync("SMS-2026-DIT-000001");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Certificate not found or invalid.", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyByToken_ValidToken_ReturnsValidResult()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Issued,
            FinalGrade = "B",
            Classification = "Merit",
            IssueDate = DateTime.UtcNow,
            VerificationToken = "secure-token-abc123"
        };
        _certRepo.Setup(r => r.GetByVerificationTokenAsync("secure-token-abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);
        _studentRepo.Setup(r => r.GetStudentWithDetailsAsync(certificate.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { FirstName = "Jane", LastName = "Smith" });
        _offeringRepo.Setup(r => r.GetWithDetailsAsync(certificate.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseOffering { OfferingCode = "DIT-2026-08", Course = new Course { Name = "Diploma in IT" } });

        // Act
        var result = await _service.VerifyByTokenAsync("secure-token-abc123");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Certificate);
        Assert.Equal("Jane Smith", result.Certificate.StudentName);
    }

    [Fact]
    public async Task VerifyByToken_InvalidToken_ReturnsInvalid()
    {
        // Arrange
        _certRepo.Setup(r => r.GetByVerificationTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Certificate?)null);

        // Act
        var result = await _service.VerifyByTokenAsync("invalid-token");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Certificate not found or invalid.", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyByQrCode_UrlWithToken_ExtractsTokenAndVerifies()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Issued,
            IssueDate = DateTime.UtcNow,
            VerificationToken = "qr-token-456"
        };
        _certRepo.Setup(r => r.GetByVerificationTokenAsync("qr-token-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);
        _studentRepo.Setup(r => r.GetStudentWithDetailsAsync(certificate.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { FirstName = "Test", LastName = "User" });
        _offeringRepo.Setup(r => r.GetWithDetailsAsync(certificate.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseOffering { OfferingCode = "DIT-2026-08", Course = new Course { Name = "Diploma in IT" } });

        // Act
        var result = await _service.VerifyByQrCodeAsync("https://school.edu/verify?token=qr-token-456");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Certificate);
        Assert.Equal("SMS-2026-DIT-000001", result.Certificate.CertificateNumber);
    }

    [Fact]
    public async Task VerifyByQrCode_RawToken_VerifiesDirectly()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Issued,
            IssueDate = DateTime.UtcNow,
            VerificationToken = "raw-token-789"
        };
        _certRepo.Setup(r => r.GetByVerificationTokenAsync("raw-token-789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);
        _studentRepo.Setup(r => r.GetStudentWithDetailsAsync(certificate.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { FirstName = "Test", LastName = "User" });
        _offeringRepo.Setup(r => r.GetWithDetailsAsync(certificate.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseOffering { OfferingCode = "DIT-2026-08", Course = new Course { Name = "Diploma in IT" } });

        // Act
        var result = await _service.VerifyByQrCodeAsync("raw-token-789");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task VerifyByQrCode_InvalidData_ReturnsInvalid()
    {
        // Arrange
        _certRepo.Setup(r => r.GetByVerificationTokenAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Certificate?)null);

        // Act
        var result = await _service.VerifyByQrCodeAsync("invalid");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Certificate not found or invalid.", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyByCertificateNumber_SupersededCertificate_ReturnsInvalid()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Superseded,
            IssueDate = DateTime.UtcNow,
            VerificationToken = "token123"
        };
        _certRepo.Setup(r => r.GetByCertificateNumberAsync("SMS-2026-DIT-000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);

        // Act
        var result = await _service.VerifyByCertificateNumberAsync("SMS-2026-DIT-000001");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Certificate not found or invalid.", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyByCertificateNumber_LogsVerificationAttempt()
    {
        // Arrange
        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            CertificateNumber = "SMS-2026-DIT-000001",
            StudentId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            Status = CertificateStatus.Issued,
            IssueDate = DateTime.UtcNow,
            VerificationToken = "token123"
        };
        _certRepo.Setup(r => r.GetByCertificateNumberAsync("SMS-2026-DIT-000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);
        _studentRepo.Setup(r => r.GetStudentWithDetailsAsync(certificate.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { FirstName = "John", LastName = "Doe" });
        _offeringRepo.Setup(r => r.GetWithDetailsAsync(certificate.CourseOfferingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseOffering { OfferingCode = "DIT-2026-08", Course = new Course { Name = "Diploma in IT" } });

        // Act
        await _service.VerifyByCertificateNumberAsync("SMS-2026-DIT-000001");

        // Assert
        _auditRepo.Verify(r => r.AddAsync(It.IsAny<CertificateAuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
