using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using System;
using System.Linq;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Regression guard for the Phase 5A deployment blocker.
    ///
    /// <para>
    /// <c>20260923080030_AddSmsRequests</c> created five Request tables but never
    /// created <c>sms_request_attachments</c>, even though <c>RequestAttachment</c>
    /// is mapped in <c>ApplicationDbContext</c> and queried at runtime by
    /// <c>RequestRepository</c>. The defect shipped because the committed model
    /// snapshot was equally stale, and <c>Program.cs</c> explicitly ignores
    /// <c>RelationalEventId.PendingModelChangesWarning</c> at startup.
    /// </para>
    ///
    /// <para>
    /// These tests assert that the runtime model and the committed snapshot agree,
    /// which is the invariant EF Core reports through
    /// <c>dotnet ef migrations has-pending-model-changes</c>. If an entity is ever
    /// added without a matching migration, the first test fails and names the exact
    /// operation that is missing.
    /// </para>
    ///
    /// <para>
    /// Uses the design-time factory, so the Npgsql relational model is built without
    /// requiring a live database.
    /// </para>
    /// </summary>
    public class RequestMigrationModelSyncTests
    {
        private const string AttachmentsTable = "sms_request_attachments";

        private static ApplicationDbContext CreateDesignTimeContext()
            => new ApplicationDbContextFactory().CreateDbContext(Array.Empty<string>());

        /// <summary>Renders a migration operation as a short, readable description.</summary>
        private static string Describe(MigrationOperation operation) => operation switch
        {
            CreateTableOperation create => $"CreateTable \"{create.Name}\"",
            DropTableOperation drop => $"DropTable \"{drop.Name}\"",
            AddColumnOperation addColumn => $"AddColumn \"{addColumn.Name}\" on \"{addColumn.Table}\"",
            DropColumnOperation dropColumn => $"DropColumn \"{dropColumn.Name}\" on \"{dropColumn.Table}\"",
            RenameTableOperation renameTable => $"RenameTable \"{renameTable.Name}\" -> \"{renameTable.NewName}\"",
            RenameColumnOperation renameColumn => $"RenameColumn \"{renameColumn.Name}\" -> \"{renameColumn.NewName}\" on \"{renameColumn.Table}\"",
            AddPrimaryKeyOperation addKey => $"AddPrimaryKey on \"{addKey.Table}\"",
            AddForeignKeyOperation addFk => $"AddForeignKey \"{addFk.Name}\" on \"{addFk.Table}\"",
            CreateIndexOperation createIndex => $"CreateIndex \"{createIndex.Name}\" on \"{createIndex.Table}\"",
            DropIndexOperation dropIndex => $"DropIndex \"{dropIndex.Name}\"",
            _ => operation.GetType().Name
        };

        /// <summary>
        /// The core regression assertion: the committed model snapshot must describe
        /// exactly the same schema as the runtime model. Any difference means an
        /// entity exists in code with no corresponding migration, which would make
        /// the application query a table that was never created.
        /// </summary>
        [Fact]
        public void ModelSnapshot_IsInSyncWithRuntimeModel_SoNoTableIsMissingAtRuntime()
        {
            using var context = CreateDesignTimeContext();

            var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot;
            snapshot.Should().NotBeNull("the project must ship a model snapshot");

            // The snapshot builds a bare model with no provider services attached, so
            // the runtime initializer has to be applied before a relational model can
            // be derived from it. This mirrors what
            // `dotnet ef migrations has-pending-model-changes` does internally.
            var runtimeInitializer = context.GetService<IModelRuntimeInitializer>();
            var snapshotModel = runtimeInitializer.Initialize(snapshot!.Model, designTime: true);

            var differ = context.GetService<IMigrationsModelDiffer>();
            var differences = differ.GetDifferences(
                snapshotModel.GetRelationalModel(),
                context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

            var description = string.Join(
                Environment.NewLine,
                differences.Select(Describe));

            differences.Should().BeEmpty(
                "the runtime model and the committed snapshot must agree; otherwise a " +
                "DbSet can query a table that no migration creates. Pending operations:" +
                Environment.NewLine + description);
        }

        /// <summary>
        /// The request attachment table must be described by the committed snapshot,
        /// i.e. it is reachable through a migration rather than only existing in code.
        /// </summary>
        [Fact]
        public void RequestAttachment_IsPresentInSnapshot_SoTheTableIsCreatedByMigration()
        {
            using var context = CreateDesignTimeContext();

            var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot;
            snapshot.Should().NotBeNull();

            var entityType = snapshot!.Model.FindEntityType(typeof(RequestAttachment));
            entityType.Should().NotBeNull(
                "\"{0}\" must exist in the model snapshot so a migration creates it",
                AttachmentsTable);

            entityType!.GetTableName().Should().Be(AttachmentsTable);
        }

        /// <summary>
        /// Attachment rows are tenant-scoped, so the entity must carry a tenant column.
        /// This is the column the global query filter keys on, and the one any future
        /// row-level-security policy would have to reference.
        /// </summary>
        [Fact]
        public void RequestAttachment_HasTenantColumn_ForTenantIsolation()
        {
            using var context = CreateDesignTimeContext();

            var entityType = context.Model.FindEntityType(typeof(RequestAttachment));
            entityType.Should().NotBeNull();

            entityType!.FindProperty(nameof(RequestAttachment.TenantId)).Should().NotBeNull(
                "request attachments must be tenant-scoped");
        }

        /// <summary>
        /// Attachment rows are owned by exactly one request, and deleting that request
        /// must take its attachments with it.
        /// </summary>
        [Fact]
        public void RequestAttachment_HasCascadingForeignKeyToRequest()
        {
            using var context = CreateDesignTimeContext();

            var entityType = context.Model.FindEntityType(typeof(RequestAttachment));
            entityType.Should().NotBeNull();

            var foreignKey = entityType!.GetForeignKeys().Should().ContainSingle().Subject;

            foreignKey.Properties.Select(p => p.Name).Should().Contain(nameof(RequestAttachment.RequestId));
            foreignKey.PrincipalEntityType.GetTableName().Should().Be("sms_requests");
            foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }
    }
}
