using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Accommodation.Queries.Reports;
using SMS.Domain.Interfaces;
using SMS.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    /// <summary>
    /// Unit tests for the accommodation report export endpoint logic: format
    /// resolution, report-key routing, row capping and file naming. The report
    /// queries themselves are mocked so the export is verified in isolation.
    /// </summary>
    public class AccommodationReportExportTests
    {
        private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>();
        private readonly Mock<IPdfGenerator> _pdfGeneratorMock = new Mock<IPdfGenerator>();
        private readonly Mock<IExcelGenerator> _excelGeneratorMock = new Mock<IExcelGenerator>();
        private readonly ExportAccommodationReportHandler _handler;
        private ReportTableDocument? _capturedDocument;
        private string? _capturedSheetName;
        private IReadOnlyList<IReadOnlyList<string>>? _capturedExcelRows;

        public AccommodationReportExportTests()
        {
            _pdfGeneratorMock
                .Setup(g => g.GenerateTablePdfAsync(It.IsAny<ReportTableDocument>()))
                .Callback<ReportTableDocument>(d => _capturedDocument = d)
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            _excelGeneratorMock
                .Setup(g => g.GenerateTableExcelAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<IReadOnlyList<string>>>()))
                .Callback<string, IReadOnlyList<string>, IReadOnlyList<IReadOnlyList<string>>>(
                    (sheet, _, rows) =>
                    {
                        _capturedSheetName = sheet;
                        _capturedExcelRows = rows;
                    })
                .ReturnsAsync(new byte[] { 4, 5, 6 });

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetCurrentOccupancyReportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleHouseOccupancyReport());

            _handler = new ExportAccommodationReportHandler(
                _mediatorMock.Object,
                _pdfGeneratorMock.Object,
                _excelGeneratorMock.Object,
                NullLogger<ExportAccommodationReportHandler>.Instance);
        }

        private static AccommodationHouseOccupancyReportDto SampleHouseOccupancyReport()
        {
            var dto = new SMS.Application.DTOs.AccommodationHouseOccupancyReportDto
            {
                ReportKey = "current-occupancy",
                ReportTitle = "Current House Occupancy",
                GeneratedBy = "coordinator",
                GeneratedAtUtc = new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc),
                Summary = new OccupancySummaryReportRow
                {
                    TotalHouses = 2,
                    OccupiedHouses = 1,
                    EmptyHouses = 1,
                    TotalCapacity = 6,
                    OccupiedSpaces = 2,
                    AvailableSpaces = 4,
                    OccupancyPercentage = 33.33m
                }
            };

            dto.Rows.Add(new HouseOccupancyReportRow
            {
                HouseId = Guid.NewGuid(),
                HouseNumber = "A1",
                HouseName = "Sunflower Hall",
                LaneName = "Lane A",
                Status = "Occupied",
                Capacity = 3,
                OccupiedCount = 2,
                AvailableSpaces = 1,
                OccupancyStatus = "Occupied",
                CurrentOccupants = new List<HouseOccupantReportRow>
                {
                    new HouseOccupantReportRow
                    {
                        OccupantName = "John Doe",
                        OccupantNumber = "STU001",
                        OccupantType = SMS.Domain.Enums.OccupantType.Student
                    }
                }
            });

            return dto;
        }

        // ===== PDF =====

        [Fact]
        public async Task Handle_WhenPdfRequested_ShouldRenderPdfAndIncludePeriodInFileName()
        {
            var query = new ExportAccommodationReportQuery
            {
                ReportKey = "current-occupancy",
                Format = "PDF",
                FromDate = new DateTime(2026, 1, 1),
                ToDate = new DateTime(2026, 3, 31)
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.ContentType.Should().Be("application/pdf");
            result.FileName.Should().Be("Accommodation_Current_Occupancy_2026-01-01_to_2026-03-31.pdf");
            result.FileContent.Should().Equal(1, 2, 3);
            _pdfGeneratorMock.Verify(
                g => g.GenerateTablePdfAsync(It.IsAny<ReportTableDocument>()), Times.Once);
            _excelGeneratorMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenPdfRequested_ShouldBuildDocumentFromTheReportRows()
        {
            var query = new ExportAccommodationReportQuery { ReportKey = "current-occupancy", Format = "PDF" };

            await _handler.Handle(query, CancellationToken.None);

            _capturedDocument.Should().NotBeNull();
            _capturedDocument!.ReportTitle.Should().Be("Current House Occupancy");
            _capturedDocument.GeneratedBy.Should().Be("coordinator");
            _capturedDocument.Columns.Should().Contain(new[] { "Lane", "House No", "Occupants" });
            _capturedDocument.Rows.Should().HaveCount(1);
            _capturedDocument.Rows[0].Should().Contain("A1").And.Contain("John Doe (STU001)");
            _capturedDocument.SummaryLines.Should().Contain(l => l.StartsWith("Occupied spaces"));
        }

        // ===== Excel =====

        [Theory]
        [InlineData("EXCEL")]
        [InlineData("excel")]
        [InlineData("xlsx")]
        public async Task Handle_WhenExcelRequested_ShouldRenderSpreadsheet(string format)
        {
            var query = new ExportAccommodationReportQuery { ReportKey = "current-occupancy", Format = format };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.ContentType.Should()
                .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            result.FileName.Should().EndWith(".xlsx");
            result.FileContent.Should().Equal(4, 5, 6);
            _capturedSheetName.Should().Be("Current House Occupancy");
            _capturedExcelRows.Should().NotBeNull();
            _capturedExcelRows!.Should().Contain(r => r.Count > 0 && r[0] == "Generated on 2026-03-31 10:00 UTC by coordinator");
            _pdfGeneratorMock.VerifyNoOtherCalls();
        }

        // ===== Validation =====

        [Fact]
        public async Task Handle_WhenReportKeyIsUnknown_ShouldThrowValidationException()
        {
            var query = new ExportAccommodationReportQuery { ReportKey = "not-a-report", Format = "PDF" };

            var act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>().WithMessage("*Unknown accommodation report*");
        }

        [Fact]
        public async Task Handle_WhenReportKeyIsMissing_ShouldThrowValidationException()
        {
            var query = new ExportAccommodationReportQuery { ReportKey = "  ", Format = "PDF" };

            var act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>().WithMessage("*report key is required*");
        }

        [Fact]
        public async Task Handle_WhenFormatIsUnsupported_ShouldThrowValidationException()
        {
            var query = new ExportAccommodationReportQuery { ReportKey = "current-occupancy", Format = "CSV" };

            var act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>().WithMessage("*PDF or Excel*");
        }

        [Fact]
        public async Task Handle_WhenPeriodIsReversed_ShouldThrowValidationException()
        {
            var query = new ExportAccommodationReportQuery
            {
                ReportKey = "current-occupancy",
                Format = "PDF",
                FromDate = new DateTime(2026, 3, 31),
                ToDate = new DateTime(2026, 1, 1)
            };

            var act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            _pdfGeneratorMock.Verify(g => g.GenerateTablePdfAsync(It.IsAny<ReportTableDocument>()), Times.Never);
        }

        // ===== Routing / row cap =====

        [Fact]
        public async Task Handle_ShouldForwardFiltersAndCapExportRows()
        {
            GetCurrentOccupancyReportQuery? forwarded = null;
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetCurrentOccupancyReportQuery>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<AccommodationHouseOccupancyReportDto>, CancellationToken>(
                    (request, _) => forwarded = request as GetCurrentOccupancyReportQuery)
                .ReturnsAsync(SampleHouseOccupancyReport());

            var laneId = Guid.NewGuid();
            var query = new ExportAccommodationReportQuery
            {
                ReportKey = "current-occupancy",
                Format = "PDF",
                LaneId = laneId,
                Status = "Occupied",
                PageSize = 999999,
                Page = 4
            };

            await _handler.Handle(query, CancellationToken.None);

            forwarded.Should().NotBeNull();
            forwarded!.LaneId.Should().Be(laneId);
            forwarded.Status.Should().Be("Occupied");
            forwarded.Page.Should().Be(1, "an export always starts at the first page");
            forwarded.PageSize.Should().Be(
                ExportAccommodationReportHandler.MaxRows, "a single export file is always bounded");
        }
    }
}
