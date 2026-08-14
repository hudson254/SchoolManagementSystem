using Microsoft.EntityFrameworkCore;
using SMS.Certificates.Domain.Entities;

namespace SMS.Certificates.Infrastructure.Persistence;

/// <summary>
/// Extension methods for configuring certificate entities in DbContext
/// </summary>
public static class ApplicationDbContextExtensions
{
    /// <summary>
    /// Configure certificate entities in the DbContext
    /// </summary>
    public static void ConfigureCertificateEntities(this ModelBuilder modelBuilder)
    {
        // Certificate entity configuration
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.ToTable("Certificates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.CertificateNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.VerificationToken)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.FinalGrade)
                .HasMaxLength(5);

            entity.Property(e => e.Classification)
                .HasMaxLength(50);

            entity.Property(e => e.PdfPath)
                .HasMaxLength(500);

            entity.Property(e => e.QrCodePath)
                .HasMaxLength(500);

            entity.Property(e => e.RevocationReason)
                .HasMaxLength(500);

            entity.HasIndex(e => e.CertificateNumber)
                .IsUnique();

            entity.HasIndex(e => e.VerificationToken)
                .IsUnique();

            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.CourseOfferingId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IssueDate);

            // Relationships
            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CertificateTemplate entity configuration
        modelBuilder.Entity<CertificateTemplate>(entity =>
        {
            entity.ToTable("CertificateTemplates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.Property(e => e.Version)
                .HasMaxLength(20);

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.FieldMappings)
                .HasMaxLength(5000);

            entity.Property(e => e.LogoPath)
                .HasMaxLength(500);

            entity.Property(e => e.WatermarkPath)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDefault);
        });

        // DigitalSignature entity configuration
        modelBuilder.Entity<DigitalSignature>(entity =>
        {
            entity.ToTable("DigitalSignatures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ImagePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasIndex(e => new { e.Name, e.Type })
                .IsUnique();
        });

        // CertificateAuditLog entity configuration
        modelBuilder.Entity<CertificateAuditLog>(entity =>
        {
            entity.ToTable("CertificateAuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.CertificateId)
                .IsRequired();

            entity.Property(e => e.CertificateNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UserRole)
                .HasMaxLength(50);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.SessionId)
                .HasMaxLength(100);

            entity.Property(e => e.Outcome)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.HasIndex(e => e.CertificateId);
            entity.HasIndex(e => e.CertificateNumber);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
