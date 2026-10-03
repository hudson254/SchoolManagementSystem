using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Application.DTOs;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// End-to-end security regressions for D4 (the approval gate on privileged
    /// teaching actions) and D5 (object-level authorization on material deletion),
    /// exercised through the real HTTP pipeline against PostgreSQL.
    /// <para>
    /// Lecturer A and Lecturer B are BOTH approved and BOTH appointed to Unit 1, so
    /// every assertion below holds even though "teaches the unit" is true for the
    /// attacker - which is precisely what the defect relied on.
    /// </para>
    /// </summary>
    public class LecturerMaterialAuthorizationApiTests
        : IClassFixture<LecturerMaterialAuthorizationFixture>
    {
        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private readonly LecturerMaterialAuthorizationFixture _fixture;

        public LecturerMaterialAuthorizationApiTests(LecturerMaterialAuthorizationFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<StudyMaterialDto> UploadMaterialAsync(HttpClient client, Guid unitId, string title)
        {
            using var form = LecturerMaterialAuthorizationFixture.PdfUploadContent(
                $"{Guid.NewGuid():N}.pdf", title);

            var response = await client.PostAsync($"/api/v1/study-materials/unit/{unitId}", form);
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                "the lecturer is approved and appointed to this unit");

            return await response.Content.ReadFromJsonAsync<StudyMaterialDto>(JsonOptions);
        }

        // ─────────────────────────────────────────────────────────────────────
        // D4 - the approval gate
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task PendingLecturer_CannotUploadStudyMaterial()
        {
            _fixture.UsePendingLecturerIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            using var form = LecturerMaterialAuthorizationFixture.PdfUploadContent(
                "pending.pdf", "Pending lecturer material");

            var response = await client.PostAsync(
                $"/api/v1/study-materials/unit/{_fixture.UnitOneId}", form);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "a lecturer awaiting approval must not perform privileged teaching actions");
        }

        [Fact]
        public async Task PendingLecturer_CannotDeleteAnyStudyMaterial()
        {
            _fixture.UseLecturerAIdentity();
            var client = _fixture.CreateAuthenticatedClient();
            var material = await UploadMaterialAsync(client, _fixture.UnitOneId, "Material owned by Lecturer A");

            _fixture.UsePendingLecturerIdentity();
            var deleteResponse = await client.DeleteAsync(
                $"/api/v1/study-materials/{material!.Id}?unitId={_fixture.UnitOneId}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var (exists, isDeleted) = await _fixture.ReadMaterialAsync(material.Id);
            exists.Should().BeTrue();
            isDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task PendingLecturer_IsOfferedNoStudyMaterialUnits()
        {
            _fixture.UsePendingLecturerIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/v1/study-materials/my-units");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var units = await response.Content.ReadFromJsonAsync<List<StudyMaterialUnitDto>>(JsonOptions);
            units.Should().BeEmpty(
                "the selector must not advertise units whose uploads the API would refuse");
        }

        [Fact]
        public async Task PendingLecturer_CannotReadUnitMaterials()
        {
            _fixture.UseLecturerAIdentity();
            var client = _fixture.CreateAuthenticatedClient();
            await UploadMaterialAsync(client, _fixture.UnitOneId, "Approved lecturer material");

            _fixture.UsePendingLecturerIdentity();
            var response = await client.GetAsync($"/api/v1/study-materials/unit/{_fixture.UnitOneId}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ─────────────────────────────────────────────────────────────────────
        // D5 - object-level authorization on delete
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task LecturerB_CannotDeleteLecturerAsMaterial_AndTheMaterialSurvives()
        {
            _fixture.UseLecturerAIdentity();
            var client = _fixture.CreateAuthenticatedClient();
            var material = await UploadMaterialAsync(client, _fixture.UnitOneId, "Lecturer A exclusive notes");

            _fixture.UseLecturerBIdentity();
            var deleteResponse = await client.DeleteAsync(
                $"/api/v1/study-materials/{material!.Id}?unitId={_fixture.UnitOneId}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "lecturer B teaches the same unit but does not own the material");

            var (exists, isDeleted) = await _fixture.ReadMaterialAsync(material.Id);
            exists.Should().BeTrue("the rejected delete must not have removed the row");
            isDeleted.Should().BeFalse("the rejected delete must not have soft-deleted the row");
        }

        [Fact]
        public async Task LecturerA_CanDeleteTheirOwnMaterial()
        {
            _fixture.UseLecturerAIdentity();
            var client = _fixture.CreateAuthenticatedClient();
            var material = await UploadMaterialAsync(client, _fixture.UnitOneId, "Self deletable notes");

            var deleteResponse = await client.DeleteAsync(
                $"/api/v1/study-materials/{material!.Id}?unitId={_fixture.UnitOneId}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var (_, isDeleted) = await _fixture.ReadMaterialAsync(material.Id);
            isDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task LecturerA_CannotUploadToAUnitTheyWereNotAllocated()
        {
            _fixture.UseLecturerAIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            using var form = LecturerMaterialAuthorizationFixture.PdfUploadContent(
                "unassigned.pdf", "Material for a unit I do not teach");

            var response = await client.PostAsync(
                $"/api/v1/study-materials/unit/{_fixture.UnitThreeId}", form);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "Unit 3 is in the shared offering snapshot but was never allocated to Lecturer A");
        }

        [Fact]
        public async Task ApprovedLecturer_OnlySeesTheirOwnAllocatedUnitInTheSelector()
        {
            _fixture.UseLecturerAIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/v1/study-materials/my-units");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var units = await response.Content.ReadFromJsonAsync<List<StudyMaterialUnitDto>>(JsonOptions);
            units.Should().Contain(u => u.UnitId == _fixture.UnitOneId);
            units.Should().NotContain(u => u.UnitId == _fixture.UnitTwoId);
            units.Should().NotContain(u => u.UnitId == _fixture.UnitThreeId,
                "the shared offering snapshot must not leak unallocated units into the selector");
        }
    }
}
