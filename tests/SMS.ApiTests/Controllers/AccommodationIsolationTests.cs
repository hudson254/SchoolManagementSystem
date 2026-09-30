using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Tenant-isolation tests for the Accommodation module, executed against the
    /// real PostgreSQL test database (not the in-memory provider).
    ///
    /// <para>
    /// <b>What these tests prove.</b> The fixture seeds two fully populated
    /// tenants (Tenant A and Tenant B) into the same database and then issues HTTP
    /// requests as Tenant A only. A cross-tenant leak shows up as Tenant B data in
    /// a Tenant A response, or as a 200/404 that is only correct by accident. Each
    /// isolation test therefore pairs the HTTP assertion with a direct database
    /// read proving the foreign row really exists - otherwise a "pass" could just
    /// mean the seed silently failed.
    /// </para>
    ///
    /// <para>
    /// <b>Scope of the guarantee.</b> These tests exercise the application-layer
    /// isolation that EF Core query filters provide. They do NOT assert PostgreSQL
    /// Row Level Security, because RLS is currently inert: the application connects
    /// as a superuser with BYPASSRLS, so the policies created by
    /// <c>AddTenantRowLevelSecurityPolicies</c> are never evaluated. Closing that
    /// gap requires moving the application onto a NOBYPASSRLS role and then running
    /// ENABLE ROW LEVEL SECURITY.
    /// </para>
    /// </summary>
    public class AccommodationIsolationTests : IClassFixture<AccommodationApiFixture>
    {
        private readonly AccommodationApiFixture _fixture;

        public AccommodationIsolationTests(AccommodationApiFixture fixture)
        {
            _fixture = fixture;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(raw).RootElement.Clone();
        }

        /// <summary>
        /// Guard used by every isolation test: the foreign tenant's data must
        /// actually be present in PostgreSQL, otherwise "not returned" is
        /// meaningless.
        /// </summary>
        private void AssertForeignSeedIsReal()
        {
            using var db = _fixture.CreateTenantDb(AccommodationApiFixture.TenantB);

            db.Lanes.IgnoreQueryFilters()
                .Any(l => l.Id == _fixture.ForeignLaneId)
                .Should().BeTrue("the foreign lane must exist in the database for this test to mean anything");

            db.Students.IgnoreQueryFilters()
                .Any(s => s.StudentNumber == _fixture.ForeignStudentNumber)
                .Should().BeTrue("the foreign student must exist in the database");

            db.Lecturers.IgnoreQueryFilters()
                .Any(l => l.EmployeeNumber == _fixture.ForeignEmployeeNumber)
                .Should().BeTrue("the foreign lecturer must exist in the database");

            db.Houses.IgnoreQueryFilters()
                .Any(h => h.HouseNumber == _fixture.ForeignHouseNumber)
                .Should().BeTrue("the foreign free house must exist in the database");
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Seed sanity: the whole suite is worthless if seeding silently no-ops
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task Fixture_SeedsBothTenants_AndOwnTenantDataIsVisible()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync("/api/v1/accommodation/lanes");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var lanes = await ReadJsonAsync(response);
            var names = lanes.EnumerateArray()
                .Select(l => l.GetProperty("laneName").GetString())
                .ToList();

            names.Should().Contain(_fixture.OccupiedLaneName);
            names.Should().NotContain(_fixture.ForeignLaneName,
                "the lane list must be scoped to the calling tenant");
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Lane occupancy report: the endpoint most likely to leak, because it
        // takes a caller-supplied laneId straight into the repository
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task LaneOccupancyReport_ForOwnLane_Returns200WithThatLanesHouses()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/lane-occupancy/{_fixture.OccupiedLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var report = await ReadJsonAsync(response);

            report.GetProperty("laneId").GetGuid().Should().Be(_fixture.OccupiedLaneId);
            report.GetProperty("laneName").GetString().Should().Be(_fixture.OccupiedLaneName);
            report.GetProperty("totalHouses").GetInt32().Should().Be(4);
            report.GetProperty("occupied").GetInt32().Should().Be(1);
            report.GetProperty("vacant").GetInt32().Should().Be(2);
            report.GetProperty("maintenance").GetInt32().Should().Be(1);

            // Capacity 2 + 3 + 1 + 2 = 8 places, one of them taken.
            report.GetProperty("totalCapacity").GetInt32().Should().Be(8);
            report.GetProperty("occupants").GetInt32().Should().Be(1);
            report.GetProperty("availableCapacity").GetInt32().Should().Be(7);
            report.GetProperty("occupancyPercentage").GetDouble().Should().BeApproximately(12.5, 0.01);

            var houses = report.GetProperty("houses").EnumerateArray()
                .Select(h => h.GetProperty("houseNumber").GetString())
                .ToList();
            houses.Should().HaveCount(4);
        }

        [Fact]
        public async Task LaneOccupancyReport_ForForeignTenantLane_Returns404_NotTheOtherTenantsData()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/lane-occupancy/{_fixture.ForeignLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "a lane belonging to another tenant must be indistinguishable from a lane that does not exist");

            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain(_fixture.ForeignLaneName);
        }

        [Fact]
        public async Task LaneOccupancyReport_ForOwnLaneWithNoHouses_ReturnsZerosRatherThan404()
        {
            // Guards the opposite failure mode: an empty-but-existing lane must
            // not be mistaken for a missing one, or a real report would look 404.
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/lane-occupancy/{_fixture.EmptyLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var report = await ReadJsonAsync(response);

            report.GetProperty("laneId").GetGuid().Should().Be(_fixture.EmptyLaneId);
            report.GetProperty("totalHouses").GetInt32().Should().Be(0);
            report.GetProperty("totalCapacity").GetInt32().Should().Be(0);
            report.GetProperty("occupants").GetInt32().Should().Be(0);
            report.GetProperty("occupancyPercentage").GetDouble().Should().Be(0);
            report.GetProperty("houses").GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task LaneOccupancyReport_ForRandomGuid_Returns404()
        {
            using var client = await _fixture.CreateAdminClientAsync();

            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/lane-occupancy/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Student accommodation list: filtered in memory from
        // GetAssignmentsWithDetailsAsync, so it is the widest blast radius
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task StudentAccommodationList_ReturnsOwnTenantStudents_AndNeverTheOtherTenants()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync("/api/v1/accommodation/reports/student-accommodation");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);
            var numbers = rows.EnumerateArray()
                .Select(r => r.GetProperty("studentNumber").GetString())
                .ToList();

            numbers.Should().Contain(_fixture.StudentNumber,
                "the calling tenant's own student must be returned");

            numbers.Should().NotContain(_fixture.ForeignStudentNumber,
                "another tenant's student must never be returned");

            var laneNames = rows.EnumerateArray()
                .Select(r => r.GetProperty("laneName").GetString())
                .ToList();
            laneNames.Should().NotContain(_fixture.ForeignLaneName);
        }

        [Fact]
        public async Task StudentAccommodationList_ForForeignTenantLaneId_ReturnsEmptyRatherThanForeignRows()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/student-accommodation?laneId={_fixture.ForeignLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);

            rows.GetArrayLength().Should().Be(0,
                "filtering by another tenant's lane must not surface that lane's occupants");
        }

        [Fact]
        public async Task StudentAccommodationList_ForOwnLaneId_ReturnsOnlyThatLanesStudents()
        {
            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/student-accommodation?laneId={_fixture.OccupiedLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);

            rows.GetArrayLength().Should().Be(1,
                "the lane filter must actually narrow the list - an unfiltered response would be a silent bug");
            rows[0].GetProperty("studentNumber").GetString().Should().Be(_fixture.StudentNumber);
            rows[0].GetProperty("laneName").GetString().Should().Be(_fixture.OccupiedLaneName);
        }

        [Fact]
        public async Task StudentAccommodationList_SearchByForeignStudentNumber_ReturnsNothing()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/student-accommodation?searchTerm={_fixture.ForeignStudentNumber}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);

            rows.GetArrayLength().Should().Be(0,
                "searching by another tenant's student number must not disclose that the student exists");
        }

        [Fact]
        public async Task StudentAccommodationList_SearchByOwnStudentNumber_ReturnsThatStudent()
        {
            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/student-accommodation?searchTerm={_fixture.StudentNumber}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);

            rows.GetArrayLength().Should().Be(1);
            rows[0].GetProperty("studentNumber").GetString().Should().Be(_fixture.StudentNumber);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Lecturer accommodation list
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task LecturerAccommodationList_ReturnsOwnTenantLecturers_AndNeverTheOtherTenants()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync("/api/v1/accommodation/reports/lecturer-accommodation");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);
            var employeeNumbers = rows.EnumerateArray()
                .Select(r => r.GetProperty("employeeNumber").GetString())
                .ToList();

            employeeNumbers.Should().Contain(_fixture.EmployeeNumber);
            employeeNumbers.Should().NotContain(_fixture.ForeignEmployeeNumber);
        }

        [Fact]
        public async Task LecturerAccommodationList_ForForeignTenantLaneId_ReturnsEmpty()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/lecturer-accommodation?laneId={_fixture.ForeignLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);

            rows.GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task LecturerAccommodationList_ForOwnLaneId_NarrowsToThatLane()
        {
            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/reports/lecturer-accommodation?laneId={_fixture.OccupiedLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);

            rows.GetArrayLength().Should().Be(1,
                "the lane filter must narrow the lecturer list instead of returning every lane");
            rows[0].GetProperty("employeeNumber").GetString().Should().Be(_fixture.EmployeeNumber);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Lane / house listings
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task Houses_ForForeignTenantLane_ReturnsEmpty()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                $"/api/v1/accommodation/lanes/{_fixture.ForeignLaneId}/houses");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var houses = await ReadJsonAsync(response);

            houses.GetArrayLength().Should().Be(0,
                "the houses of another tenant's lane must not be listable");
        }

        [Fact]
        public async Task AvailableHouses_IncludeOwnFreeHouse_AndExcludeAnotherTenants()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync("/api/v1/accommodation/houses/available");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var houses = await ReadJsonAsync(response);
            var numbers = houses.EnumerateArray()
                .Select(h => h.GetProperty("houseNumber").GetString())
                .ToList();

            numbers.Should().Contain(_fixture.OwnFreeHouseNumber,
                "the calling tenant's own free house must be returned, otherwise the " +
                "assertion below would pass only because the endpoint returned nothing");

            numbers.Should().NotContain(_fixture.ForeignHouseNumber,
                "another tenant's free house must not be offered as bookable");
        }

        [Fact]
        public async Task LaneById_ForForeignTenantLane_Returns404()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync($"/api/v1/accommodation/lanes/{_fixture.ForeignLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Exports: the same rows reach the filesystem, so isolation must hold
        // there too. The xlsx is a zip; its cell text lives in
        // xl/sharedStrings.xml, which is plain XML and can be asserted on.
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task Export_CurrentOccupancyExcel_ContainsOwnDataAndNoForeignTenantData()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                "/api/v1/accommodation/reports/export?reportKey=current-occupancy&format=EXCEL");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType
                .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var text = ExtractXlsxSharedStrings(bytes);

            text.Should().Contain(_fixture.StudentNumber,
                "the exported report must include the calling tenant's own rows");
            text.Should().NotContain(_fixture.ForeignStudentNumber,
                "the exported file must never carry another tenant's occupants");
            text.Should().NotContain(_fixture.ForeignEmployeeNumber);
        }

        [Fact]
        public async Task Export_CurrentOccupancyExcel_ForForeignTenantLane_ExportsNoRows()
        {
            AssertForeignSeedIsReal();

            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                "/api/v1/accommodation/reports/export?reportKey=current-occupancy&format=EXCEL" +
                $"&laneId={_fixture.ForeignLaneId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var text = ExtractXlsxSharedStrings(bytes);

            text.Should().NotContain(_fixture.ForeignStudentNumber,
                "exporting another tenant's lane filter must not export that lane's occupants");
        }

        [Fact]
        public async Task Export_UnknownReportKey_Returns400()
        {
            using var client = await _fixture.CreateAdminClientAsync();
            var response = await client.GetAsync(
                "/api/v1/accommodation/reports/export?reportKey=not-a-real-report&format=EXCEL");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Authorization: tenant isolation is meaningless if a role without
        // accommodation access can read the data at all
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task StudentRole_CannotReadAccommodationReports()
        {
            using var client = await _fixture.CreateClientWithRoleAsync("Student");

            var response = await client.GetAsync("/api/v1/accommodation/reports/student-accommodation");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "ReceptionistAccess must reject a Student regardless of tenant");
        }

        [Fact]
        public async Task ReceptionistRole_CanReadOwnTenantAccommodationReports()
        {
            using var client = await _fixture.CreateClientWithRoleAsync("Receptionist");

            var response = await client.GetAsync("/api/v1/accommodation/reports/student-accommodation");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = await ReadJsonAsync(response);
            rows.EnumerateArray()
                .Select(r => r.GetProperty("studentNumber").GetString())
                .Should().NotContain(_fixture.ForeignStudentNumber);
        }

        [Fact]
        public async Task AnonymousCaller_CannotReadAccommodationReports()
        {
            using var client = _fixture.CreateClient();

            var response = await client.GetAsync("/api/v1/accommodation/reports/student-accommodation");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Reads the shared-string table out of an xlsx package so assertions can
        /// be made on the actual cell text rather than on opaque binary bytes.
        /// </summary>
        private static string ExtractXlsxSharedStrings(byte[] xlsxBytes)
        {
            using var stream = new MemoryStream(xlsxBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return string.Empty;

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
