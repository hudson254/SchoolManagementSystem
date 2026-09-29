using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Domain.Interfaces;
using SMS.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Reporting.Services
{
    public class PdfGeneratorService : IPdfGenerator
    {
        public PdfGeneratorService()
        {
            // Ensure QuestPDF is licensed for free tier
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent)
        {
            // QuestPDF doesn't handle HTML directly, so we create a document with the content
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Element(c => Header(c, "Document"));
                    page.Content().Column(column =>
                    {
                        column.Item().Text(htmlContent);
                    });
                    page.Footer().Element(c => Footer(c));
                });
            });

            var bytes = document.GeneratePdf();
            return await Task.FromResult(bytes);
        }

        public async Task<byte[]> GenerateTranscriptPdfAsync(object transcriptData)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Element(c => Header(c, "Academic Transcript"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("Student Name: [Name]").FontSize(12);
                        column.Item().Text("Student Number: [Number]").FontSize(12);
                        column.Item().Text("Programme: [Programme]").FontSize(12);
                        column.Item().LineHorizontal(1);
                        column.Item().Text("Courses:").FontSize(12).Bold();

                        // Sample course entries
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#").Bold();
                                header.Cell().Text("Course").Bold();
                                header.Cell().Text("Grade").Bold();
                                header.Cell().Text("Credits").Bold();
                            });

                            table.Cell().Text("1");
                            table.Cell().Text("Course Name");
                            table.Cell().Text("A");
                            table.Cell().Text("3");
                        });

                        column.Item().LineHorizontal(1);
                        column.Item().AlignRight().Text("Cumulative GPA: 4.0").FontSize(12).Bold();
                    });

                    page.Footer().Element(c => Footer(c));
                });
            });

            var bytes = document.GeneratePdf();
            return await Task.FromResult(bytes);
        }

        public async Task<byte[]> GenerateReportPdfAsync(object reportData)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Element(c => Header(c, "Report"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Text("Report generated on: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(10);
                        column.Item().LineHorizontal(1);
                        column.Item().Text("Report content goes here...");
                    });

                    page.Footer().Element(c => Footer(c));
                });
            });

            var bytes = document.GeneratePdf();
            return await Task.FromResult(bytes);
        }

        public async Task<byte[]> GenerateTablePdfAsync(ReportTableDocument document)
        {
            var doc = document ?? throw new ArgumentNullException(nameof(document));

            var bytes = await Task.Run(() => Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(t => t.FontSize(9));

                    page.Header().Column(header =>
                    {
                        header.Item().Text(doc.SystemName).FontSize(15).Bold();
                        header.Item().Text(doc.ReportTitle).FontSize(12).SemiBold();
                        header.Item().Text($"Generated on {doc.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC by {doc.GeneratedBy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        if (doc.Filters.Count > 0)
                        {
                            header.Item().PaddingTop(4).Text(string.Join("  |  ", doc.Filters.Select(f => $"{f.Label}: {f.Value}"))).FontSize(8);
                        }
                        if (doc.SummaryLines.Count > 0)
                        {
                            header.Item().PaddingTop(4).Text(string.Join("  |  ", doc.SummaryLines)).FontSize(8).SemiBold();
                        }
                        header.Item().PaddingTop(6).LineHorizontal(1);
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        var columnCount = Math.Max(1, doc.Columns.Count);
                        table.ColumnsDefinition(columns =>
                        {
                            for (var i = 0; i < columnCount; i++)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var title in doc.Columns)
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(title).Bold().FontSize(8);
                            }
                        });

                        foreach (var row in doc.Rows)
                        {
                            for (var i = 0; i < columnCount; i++)
                            {
                                var value = i < row.Count ? row[i] : string.Empty;
                                table.Cell().Padding(4).Text(value ?? string.Empty).FontSize(8);
                            }
                        }

                        if (doc.Rows.Count == 0)
                        {
                            table.Cell().ColumnSpan((uint)columnCount).Padding(6)
                                .Text("No records matched the selected filters.").Italic();
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                        row.RelativeItem().AlignRight().Text(doc.SystemName).FontSize(8);
                    });
                });
            }).GeneratePdf());

            return bytes;
        }

        private static void Header(IContainer container, string title)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("School Management System").FontSize(16).Bold();
                    column.Item().Text(title).FontSize(12);
                });

                row.ConstantItem(100).Height(50).Placeholder();
            });
        }

        private static void Footer(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });

                row.RelativeItem().AlignRight().Text("Generated by SMS Reporting");
            });
        }
    }
}

