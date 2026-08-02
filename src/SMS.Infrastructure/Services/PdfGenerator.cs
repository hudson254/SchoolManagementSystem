using SMS.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SMS.Infrastructure.Services
{
    public class PdfGenerator : IPdfGenerator
    {
        private readonly ILogger<PdfGenerator> _logger;

        public PdfGenerator(ILogger<PdfGenerator> logger)
        {
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateReportAsync<T>(T data, string templateName)
        {
            try
            {
                var document = templateName switch
                {
                    "Transcript" => GenerateTranscript(data as dynamic),
                    "Report" => GenerateGenericReport(data as dynamic),
                    _ => GenerateGenericReport(data)
                };

                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF report");
                throw;
            }
        }

        private IDocument GenerateTranscript(dynamic data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text("Academic Transcript")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item().Text($"Student: {data.StudentName}");
                            column.Item().Text($"Student Number: {data.StudentNumber}");
                            column.Item().Text($"Programme: {data.ProgrammeName}");
                            column.Item().Text($"Cumulative GPA: {data.CumulativeGPA:F2}");

                            column.Item().LineHorizontal(1);

                            // Grade table
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Unit").Bold();
                                    header.Cell().Text("Code").Bold();
                                    header.Cell().Text("Credits").Bold();
                                    header.Cell().Text("Grade").Bold();
                                    header.Cell().Text("Points").Bold();
                                });

                                foreach (var grade in data.AllGrades)
                                {
                                    table.Cell().Text(grade.UnitName);
                                    table.Cell().Text(grade.UnitCode);
                                    table.Cell().Text(grade.Credits.ToString());
                                    table.Cell().Text(grade.Grade);
                                    table.Cell().Text(grade.GradePoints?.ToString("F2") ?? "-");
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated on: ");
                            x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
                        });
                });
            });
        }

        private IDocument GenerateGenericReport(dynamic data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header()
                        .Text("School Management System Report")
                        .FontSize(18)
                        .Bold()
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);
                            column.Item().Text(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
                });
            });
        }
    }
}