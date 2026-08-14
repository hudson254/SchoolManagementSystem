using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SMS.Certificates.API.Extensions;
using SMS.Certificates.Application.Services;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;

namespace SMS.Certificates.API.Controllers;

/// <summary>
/// Certificate management endpoints
/// </summary>
[ApiController]
[Route("api/v1/certificates")]
[Produces("application/json")]
[Authorize(Policy = "ModeratorAccess")]
public class CertificateController : ControllerBase
{
    private readonly CertificateService _certificateService;
    private readonly ICertificateRepository _certificateRepository;
    private readonly BulkCertificateService _bulkCertificateService;
    private readonly ILogger<CertificateController> _logger;

    public CertificateController(
        CertificateService certificateService,
        ICertificateRepository certificateRepository,
        BulkCertificateService bulkCertificateService,
        ILogger<CertificateController> logger)
    {
        _certificateService = certificateService;
        _certificateRepository = certificateRepository;
        _bulkCertificateService = bulkCertificateService;
        _logger = logger;
    }

    /// <summary>
    /// Generate certificate for a student
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(Certificate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Certificate>> GenerateCertificate(
        [FromBody] GenerateCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var certificate = await _certificateService.GenerateCertificateAsync(
                request.StudentId,
                request.CourseOfferingId,
                request.TemplateId,
                User.GetUserId(),
                User.GetUserRole(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Session.Id,
                cancellationToken);

            if (certificate == null)
            {
                return BadRequest(new { message = "Student is not eligible for certificate" });
            }

            return Ok(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error generating certificate" });
        }
    }

    /// <summary>
    /// Revoke a certificate
    /// </summary>
    [HttpPost("{id}/revoke")]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeCertificate(
        Guid id,
        [FromBody] RevokeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _certificateService.RevokeCertificateAsync(
                id,
                request.Reason,
                User.GetUserId(),
                User.GetUserRole(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Session.Id,
                cancellationToken);

            return Ok(new { message = "Certificate revoked successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking certificate {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error revoking certificate" });
        }
    }

    /// <summary>
    /// Regenerate a certificate
    /// </summary>
    [HttpPost("{id}/regenerate")]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(typeof(Certificate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Certificate>> RegenerateCertificate(
        Guid id,
        [FromBody] RegenerateCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var certificate = await _certificateService.RegenerateCertificateAsync(
                id,
                request.Reason,
                User.GetUserId(),
                User.GetUserRole(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Session.Id,
                cancellationToken);

            if (certificate == null)
            {
                return NotFound(new { message = "Certificate not found" });
            }

            return Ok(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating certificate {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error regenerating certificate" });
        }
    }

    /// <summary>
    /// Get certificate by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Certificate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Certificate>> GetCertificate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var certificate = await _certificateRepository.GetByIdAsync(id, cancellationToken);
            if (certificate == null)
            {
                return NotFound(new { message = "Certificate not found" });
            }

            return Ok(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error getting certificate" });
        }
    }

    /// <summary>
    /// Download certificate PDF
    /// </summary>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var certificate = await _certificateRepository.GetByIdAsync(id, cancellationToken);
            if (certificate == null)
            {
                return NotFound(new { message = "Certificate not found" });
            }

            if (string.IsNullOrEmpty(certificate.PdfPath) || !System.IO.File.Exists(certificate.PdfPath))
            {
                return NotFound(new { message = "Certificate PDF not found" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(certificate.PdfPath, cancellationToken);
            var fileName = $"{certificate.CertificateNumber}.pdf";
            return File(fileBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading certificate {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error downloading certificate" });
        }
    }

    /// <summary>
    /// Get certificates for a student
    /// </summary>
    [HttpGet("student/{studentId}")]
    [ProducesResponseType(typeof(IEnumerable<Certificate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Certificate>>> GetStudentCertificates(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var certificates = await _certificateRepository.GetByStudentIdAsync(studentId, cancellationToken);
            return Ok(certificates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificates for student {StudentId}", studentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error getting certificates" });
        }
    }

    /// <summary>
    /// Bulk generate certificates for all eligible students in a course offering
    /// </summary>
    [HttpPost("bulk/generate")]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(typeof(BulkGenerationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkGenerationResult>> BulkGenerateCertificates(
        [FromBody] BulkGenerateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bulkCertificateService.GenerateForCourseOfferingAsync(
                request.CourseOfferingId,
                User.GetUserId(),
                User.GetUserRole(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Session.Id,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk certificate generation for offering {CourseOfferingId}", request.CourseOfferingId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error in bulk certificate generation" });
        }
    }

    /// <summary>
    /// Bulk generate certificates for all eligible students across all completed offerings
    /// </summary>
    [HttpPost("bulk/generate-all")]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(typeof(BulkGenerationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkGenerationResult>> BulkGenerateAllCertificates(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bulkCertificateService.GenerateForAllCompletedOfferingsAsync(
                User.GetUserId(),
                User.GetUserRole(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Session.Id,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk certificate generation for all completed offerings");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error in bulk certificate generation" });
        }
    }

    /// <summary>
    /// Search and list certificates with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> SearchCertificates(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? courseOfferingId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _certificateRepository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, status, studentId, courseOfferingId, cancellationToken);

            return Ok(new
            {
                items = result.Items,
                totalCount = result.TotalCount,
                pageNumber,
                pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching certificates");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching certificates" });
        }
    }
}

#region Request DTOs

public class GenerateCertificateRequest
{
    public Guid StudentId { get; set; }
    public Guid CourseOfferingId { get; set; }
    public Guid? TemplateId { get; set; }
}

public class RevokeCertificateRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class RegenerateCertificateRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class BulkGenerateRequest
{
    public Guid CourseOfferingId { get; set; }
}

#endregion
