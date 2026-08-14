using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SMS.Certificates.API.Extensions;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Interfaces;

namespace SMS.Certificates.API.Controllers;

[ApiController]
[Route("api/v1/certificates/templates")]
[Produces("application/json")]
[Authorize(Policy = "ModeratorAccess")]
public class CertificateTemplateController : ControllerBase
{
    private readonly ICertificateTemplateRepository _templateRepository;
    private readonly ILogger<CertificateTemplateController> _logger;

    public CertificateTemplateController(
        ICertificateTemplateRepository templateRepository,
        ILogger<CertificateTemplateController> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CertificateTemplate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CertificateTemplate>>> GetAll(CancellationToken ct)
    {
        var templates = await _templateRepository.GetAllAsync(ct);
        return Ok(templates);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CertificateTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateTemplate>> GetById(Guid id, CancellationToken ct)
    {
        var template = await _templateRepository.GetByIdAsync(id, ct);
        if (template == null) return NotFound(new { message = "Template not found" });
        return Ok(template);
    }

    [HttpPost]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(typeof(CertificateTemplate), StatusCodes.Status201Created)]
    public async Task<ActionResult<CertificateTemplate>> Create(
        [FromBody] CertificateTemplateRequest request, CancellationToken ct)
    {
        var exists = await _templateRepository.TemplateNameExistsAsync(request.Name, ct);
        if (exists) return BadRequest(new { message = "Template name already exists" });

        var template = new CertificateTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Version = request.Version ?? "1.0",
            Type = request.Type,
            Status = request.Status ?? "Active",
            CourseId = request.CourseId,
            FilePath = request.FilePath,
            LogoPath = request.LogoPath,
            WatermarkPath = request.WatermarkPath,
            FieldMappings = request.FieldMappings ?? "{}",
            IsDefault = request.IsDefault ?? false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.GetUserId() ?? Guid.Empty,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = User.GetUserId() ?? Guid.Empty
        };

        var created = await _templateRepository.AddAsync(template, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(typeof(CertificateTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateTemplate>> Update(
        Guid id, [FromBody] CertificateTemplateRequest request, CancellationToken ct)
    {
        var template = await _templateRepository.GetByIdAsync(id, ct);
        if (template == null) return NotFound(new { message = "Template not found" });

        template.Name = request.Name;
        template.Description = request.Description;
        template.Version = request.Version ?? template.Version;
        template.Type = request.Type;
        template.Status = request.Status ?? template.Status;
        template.CourseId = request.CourseId;
        template.FilePath = request.FilePath;
        template.LogoPath = request.LogoPath;
        template.WatermarkPath = request.WatermarkPath;
        template.FieldMappings = request.FieldMappings ?? template.FieldMappings;
        template.IsDefault = request.IsDefault ?? template.IsDefault;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = User.GetUserId() ?? Guid.Empty;

        await _templateRepository.UpdateAsync(template, ct);
        return Ok(template);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdministratorAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _templateRepository.DeleteAsync(id, ct);
        return NoContent();
    }
}

public class CertificateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Status { get; set; }
    public Guid? CourseId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? WatermarkPath { get; set; }
    public string? FieldMappings { get; set; }
    public bool? IsDefault { get; set; }
}
