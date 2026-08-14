using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SMS.Certificates.Domain.Interfaces;
using SMS.Certificates.Domain.Entities;

namespace SMS.Certificates.API.Controllers;

/// <summary>
/// Public certificate verification endpoints
/// </summary>
[ApiController]
[Route("api/v1/verify")]
[Produces("application/json")]
public class VerificationController : ControllerBase
{
    private readonly ICertificateVerificationService _verificationService;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(
        ICertificateVerificationService verificationService,
        ILogger<VerificationController> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    /// <summary>
    /// Verify a certificate by certificate number
    /// </summary>
    /// <remarks>
    /// This is a public endpoint for certificate verification.
    /// Example: GET /api/v1/verify/certificate/SMS-2026-DIT-000001
    /// </remarks>
    [HttpGet("certificate/{certificateNumber}")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerificationResult>> VerifyByCertificateNumber(
        string certificateNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _verificationService.VerifyByCertificateNumberAsync(certificateNumber, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate {CertificateNumber}", certificateNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error verifying certificate" });
        }
    }

    /// <summary>
    /// Verify a certificate by verification token
    /// </summary>
    /// <remarks>
    /// This is a public endpoint for certificate verification.
    /// Example: GET /api/v1/verify/token/abc123
    /// </remarks>
    [HttpGet("token/{verificationToken}")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerificationResult>> VerifyByToken(
        string verificationToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _verificationService.VerifyByTokenAsync(verificationToken, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate by token");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error verifying certificate" });
        }
    }

    /// <summary>
    /// Verify a certificate by QR code data
    /// </summary>
    /// <remarks>
    /// This endpoint accepts QR code data (URL or token) and verifies the certificate.
    /// Example: POST /api/v1/verify/qrcode
    /// Body: { "qrCodeData": "https://school.edu/verify?token=abc123" }
    /// </remarks>
    [HttpPost("qrcode")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerificationResult>> VerifyByQrCode(
        [FromBody] VerifyQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _verificationService.VerifyByQrCodeAsync(request.QrCodeData, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying certificate by QR code");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error verifying certificate" });
        }
    }
}

#region Request DTOs

public class VerifyQrCodeRequest
{
    /// <summary>
    /// QR code data (URL or token)
    /// </summary>
    public string QrCodeData { get; set; } = string.Empty;
}

#endregion
