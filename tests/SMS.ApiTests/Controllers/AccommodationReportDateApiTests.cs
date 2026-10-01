using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Regression tests for the occupancy-history date/time defect: a request
    /// that carried <c>DateTimeKind.Unspecified</c> into the PostgreSQL
    /// <c>timestamp with time zone</c> comparison returned HTTP 500
    /// ("Cannot write DateTime with Kind=Unspecified").
    ///
    /// <para>
    /// These are true HTTP tests against the real PostgreSQL database: the whole
    /// point of the defect was that it only appeared once the bound crossed into
    /// EF Core / Npgsql, so testing the handler directly would prove nothing. Each
    /// request below sends the date exactly as a client would put it in the query
    /// string.
    /// </para>
    /// </summary>
    public class AccommodationReportDateApiTests : IClassFixture<AccommodationApiFixture>
    {
        private const string HistoryPath = "/api/v1/accommodation/reports/occupancy-history";
        private const string ByPeriodPath = "/api/v1/accommodation/reports/occupancy-by-period";

        private readonly AccommodationApiFixture _fixture;

        public AccommodationReportDateApiTests(AccommodationApiFixture fixture)
        {
            _fixture = fixture;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(raw).RootElement.Clone();
        }

        private static string HistoryUrl(string from, string to) =>
            $"{HistoryPath}?fromDate={Uri.EscapeDataString(from)}&toDate={Uri.EscapeDataString(to)}";

        /// <summary>
        /// The reported failure, verbatim: plain local-style date/times used to
        /// return 500. They must now return 200.
        /// </summary>
        [Theory]
        [InlineData("2026-10-01T00:00:00", "2026-10-31T00:00:00")]
        [InlineData("2026-10-01T00:00:00.000", "2026-10-31T23:59:59.999")]
        [InlineData("2026-10-01T12:00:00", "2026-10-31T12:00:00")]
        public async Task OccupancyHistory_PlainDateTimesWithoutTimezone_ShouldReturn200(string from, string to)
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(HistoryUrl(from, to));

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "a date without a UTC designator must not reach Npgsql as Kind=Unspecified");
        }

        [Theory]
        [InlineData("2026-10-01T00:00:00Z", "2026-10-31T00:00:00Z")]
        [InlineData("2026-10-01T00:00:00", "2026-10-31T00:00:00")]
        [InlineData("2026-10-01T00:00:00Z", "2026-10-31T00:00:00")]
        [InlineData("2026-10-01T00:00:00", "2026-10-31T00:00:00Z")]
        [InlineData("2026-10-01T00:00:00+03:00", "2026-10-31T00:00:00+03:00")]
        [InlineData("2026-10-01T00:00:00-05:00", "2026-10-31T00:00:00-05:00")]
        public async Task OccupancyHistory_AnyTimezoneRepresentation_ShouldReturn200(string from, string to)
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(HistoryUrl(from, to));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        [Fact]
        public async Task OccupancyHistory_ShouldEchoThePeriodBackAsUtc()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(HistoryUrl("2026-10-01T00:00:00", "2026-10-31T00:00:00"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var report = await ReadJsonAsync(response);

            var periodStart = report.GetProperty("periodStart").GetString();
            var periodEnd = report.GetProperty("periodEnd").GetString();

            periodStart.Should().NotBeNull();
            periodStart.Should().EndWith("Z", "the API contract exposes the bound in UTC");
            periodEnd.Should().EndWith("Z");

            // AdjustToUniversal so the assertion does not depend on the test host's
            // own timezone: the API must echo back the same UTC instant it received.
            var parsed = DateTime.Parse(periodStart!, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal
                | System.Globalization.DateTimeStyles.AssumeUniversal);

            parsed.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                "normalisation must not shift the calendar day the caller asked for");
        }

        [Fact]
        public async Task OccupancyHistory_SamePeriodInDifferentRepresentations_ShouldAgree()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var plain = await client.GetAsync(HistoryUrl("2026-10-01T00:00:00", "2026-10-31T00:00:00"));
            var utc = await client.GetAsync(HistoryUrl("2026-10-01T00:00:00Z", "2026-10-31T00:00:00Z"));

            plain.StatusCode.Should().Be(HttpStatusCode.OK);
            utc.StatusCode.Should().Be(HttpStatusCode.OK);

            var plainBody = await ReadJsonAsync(plain);
            var utcBody = await ReadJsonAsync(utc);

            plainBody.GetProperty("rows").GetArrayLength()
                .Should().Be(utcBody.GetProperty("rows").GetArrayLength(),
                    "the plain form and the Z form describe the same UTC period");
            plainBody.GetProperty("distinctOccupants").GetInt32()
                .Should().Be(utcBody.GetProperty("distinctOccupants").GetInt32());
            plainBody.GetProperty("distinctHouses").GetInt32()
                .Should().Be(utcBody.GetProperty("distinctHouses").GetInt32());
        }

        [Fact]
        public async Task OccupancyHistory_ReversedRange_ShouldReturn400Not500()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                HistoryUrl("2026-10-31T00:00:00", "2026-10-01T00:00:00"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "an invalid range must surface the application's validation response, not a database exception");
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task OccupancyHistory_ReversedRangeWithZ_ShouldAlsoReturn400()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                HistoryUrl("2026-10-31T00:00:00Z", "2026-10-01T00:00:00Z"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("")]
        [InlineData("?fromDate=2026-10-01T00:00:00")]
        [InlineData("?toDate=2026-10-31T00:00:00")]
        public async Task OccupancyHistory_MissingBound_ShouldReturn400Not500(string query)
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync($"{HistoryPath}{query}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "this report requires both bounds; that is a validation 400, not a 500");
        }

        [Fact]
        public async Task OccupancyHistory_SameDayRange_ShouldReturn200()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                HistoryUrl("2026-10-01T00:00:00", "2026-10-01T00:00:00"));

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "fromDate == toDate is a valid single-day range");
        }

        [Fact]
        public async Task OccupancyHistory_OpenEndedOccupancyInRange_ShouldBeIncluded()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            // A wide window that certainly contains the seeded "still occupying"
            // assignments (VacatedDate IS NULL).
            var response = await client.GetAsync(
                HistoryUrl("2000-01-01T00:00:00", "2999-12-31T00:00:00"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var report = await ReadJsonAsync(response);

            report.GetProperty("rows").EnumerateArray()
                .Should().Contain(r => r.GetProperty("isCurrent").GetBoolean(),
                    "an occupant who never vacated is open-ended occupancy and must still be reported");
        }

        [Fact]
        public async Task OccupancyHistory_BoundaryDates_AroundSeededStay_ShouldBeInclusive()
        {
            using var db = _fixture.CreateTenantDb(AccommodationApiFixture.TenantA);

            // No IgnoreQueryFilters: the global tenant filter scopes this to
            // Tenant A, so the stay we assert on is one the HTTP call can see.
            var seeded = db.AccommodationAssignments
                .Where(a => a.VacatedDate == null)
                .OrderBy(a => a.AssignmentDate)
                .FirstOrDefault();
            seeded.Should().NotBeNull("the fixture seeds a current assignment for this boundary test");

            // Mirror the repository's occupancy-start rule (MoveInDate wins).
            var start = seeded!.MoveInDate
                ?? (seeded.AssignmentDate != default ? seeded.AssignmentDate : seeded.AssignedDate);

            var boundary = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var occupantId = seeded.OccupantType == SMS.Domain.Enums.OccupantType.Student
                ? seeded.StudentId
                : seeded.LecturerId;

            using var client = await _fixture.CreateAdminClientAsync();

            // Exactly the occupancy start date on both bounds: the established
            // interval-overlap rule is inclusive at both ends, and a date-only
            // end bound still covers the whole day.
            var response = await client.GetAsync(HistoryUrl(
                boundary.ToString("yyyy-MM-dd"),
                boundary.ToString("yyyy-MM-dd")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var report = await ReadJsonAsync(response);

            report.GetProperty("rows").EnumerateArray()
                .Select(r => r.GetProperty("occupantId").GetGuid())
                .Should().Contain(occupantId!.Value,
                    "a stay that begins exactly on the period boundary is inside the inclusive period");
        }

        /// <summary>
        /// The same defect reached the by-period report through the same bound,
        /// because both handlers feed the same normalised filters object.
        /// </summary>
        [Theory]
        [InlineData("2026-10-01T00:00:00", "2026-10-31T00:00:00")]
        [InlineData("2026-10-01T00:00:00Z", "2026-10-31T00:00:00Z")]
        [InlineData("2026-10-01T00:00:00+03:00", "2026-10-31T00:00:00+03:00")]
        public async Task OccupancyByPeriod_AnyRepresentation_ShouldReturn200(string from, string to)
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"{ByPeriodPath}?fromDate={Uri.EscapeDataString(from)}&toDate={Uri.EscapeDataString(to)}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("")]
        [InlineData("?fromDate=2026-10-01T00:00:00")]
        [InlineData("?toDate=2026-10-31T00:00:00")]
        [InlineData("?fromDate=2026-10-01T00:00:00Z")]
        [InlineData("?toDate=2026-10-31T00:00:00Z")]
        public async Task OccupancyByPeriod_OpenEndedBounds_ShouldReturn200(string query)
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync($"{ByPeriodPath}{query}");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "a missing bound falls back to MinValue/MaxValue, which must also be UTC-tagged");
        }

        [Fact]
        public async Task OccupancyByPeriod_ReversedRange_ShouldReturn400Not500()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"{ByPeriodPath}?fromDate=2026-10-31T00:00:00&toDate=2026-10-01T00:00:00");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// The date repair must not have weakened tenant isolation: a Tenant A
        /// request still never returns Tenant B's occupancy rows, whatever date
        /// representation it uses.
        /// </summary>
        [Theory]
        [InlineData("2026-10-01T00:00:00", "2026-10-31T00:00:00")]
        [InlineData("2000-01-01T00:00:00Z", "2999-12-31T00:00:00Z")]
        [InlineData("2000-01-01T00:00:00+03:00", "2999-12-31T00:00:00+03:00")]
        public async Task OccupancyHistory_DateRepair_MustNotLeakForeignTenantOccupancy(string from, string to)
        {
            // Guard: the foreign tenant must really own occupancy rows, otherwise
            // "not returned" would be vacuous.
            using var foreignDb = _fixture.CreateTenantDb(AccommodationApiFixture.TenantB);
            foreignDb.AccommodationAssignments.IgnoreQueryFilters()
                .Any(a => a.StudentId != null || a.LecturerId != null)
                .Should().BeTrue("the foreign tenant must own occupancy rows for this test to mean anything");

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(HistoryUrl(from, to));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();

            body.Should().NotContain(_fixture.ForeignStudentNumber,
                "the report must stay scoped to the caller's tenant after the date repair");
            body.Should().NotContain(_fixture.ForeignEmployeeNumber);
        }

        [Fact]
        public async Task OccupancyHistory_ForForeignLaneFilter_MustNotReturnForeignOccupants()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"{HistoryUrl("2000-01-01T00:00:00", "2999-12-31T00:00:00")}&laneId={_fixture.ForeignLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();

            body.Should().NotContain(_fixture.ForeignStudentNumber);
            body.Should().NotContain(_fixture.ForeignEmployeeNumber);
        }
    }
}