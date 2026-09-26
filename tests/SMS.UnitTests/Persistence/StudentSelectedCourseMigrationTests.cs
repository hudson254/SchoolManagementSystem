using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SMS.Persistence.Data;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SMS.UnitTests.Persistence
{
    /// <summary>
    /// Guards the student course-selection fix migration
    /// (<c>AddStudentSelectedCourse</c>). The reported production bug was that
    /// the course chosen at registration was validated and then discarded
    /// (<c>RegisterCommand.CreateStudentRecord</c> built the Student with only
    /// <c>ProgrammeId</c>). This migration adds the missing, nullable
    /// <c>Students.SelectedCourseId</c> column plus a guarded backfill.
    ///
    /// <para>
    /// The schema change must stay strictly additive and fully reversible: no
    /// existing column is dropped or rewritten, and the column is nullable so
    /// pre-existing students remain valid. RLS is unaffected because the tenant
    /// policies created by <c>AddTenantRowLevelSecurityPolicies</c> are
    /// column-independent and the new column lives on the already-covered
    /// <c>Students</c> table.
    /// </para>
    /// </summary>
    public class StudentSelectedCourseMigrationTests
    {
        private const string MigrationId = "20260926183852_AddStudentSelectedCourse";

        private static ApplicationDbContext CreateDesignTimeContext()
            => new ApplicationDbContextFactory().CreateDbContext(Array.Empty<string>());

        private static MigrationBuilder BuildOperations(Migration migration, bool up)
        {
            var builder = new MigrationBuilder("Npgsql");
            var method = typeof(Migration).GetMethod(
                up ? "Up" : "Down",
                BindingFlags.Instance | BindingFlags.NonPublic);

            method.Should().NotBeNull("EF Core exposes Up/Down as protected methods");
            method!.Invoke(migration, new object[] { builder });

            return builder;
        }

        private static string RunMigration(Migration migration, bool up) =>
            string.Join(
                Environment.NewLine,
                BuildOperations(migration, up).Operations.OfType<SqlOperation>().Select(o => o.Sql));

        private static MigrationBuilder RunAgainstMigratedMigration(bool up)
        {
            using var context = CreateDesignTimeContext();
            var migrations = context.GetService<IMigrationsAssembly>().Migrations;
            migrations.Should().ContainKey(MigrationId);

            // Migrations is keyed by name and holds the concrete Migration type.
            var migration = (Migration)Activator.CreateInstance(
                migrations[MigrationId].AsType())!;

            return BuildOperations(migration, up);
        }

        /// <summary>The migration must be discoverable by EF Core, or it never runs.</summary>
        [Fact]
        public void SelectedCourseMigration_IsDiscoverableByEfCore()
        {
            using var context = CreateDesignTimeContext();

            var migrations = context.GetService<IMigrationsAssembly>().Migrations;

            migrations.Should().ContainKey(MigrationId,
                "a migration EF Core cannot discover is a migration that never runs in production");
        }

        /// <summary>
        /// Up() adds the column as NULLABLE, which is what keeps the change
        /// additive and keeps pre-fix student rows valid.
        /// </summary>
        [Fact]
        public void Up_AddsSelectedCourseIdAsANullableColumn()
        {
            var builder = RunAgainstMigratedMigration(up: true);

            var addColumn = builder.Operations
                .OfType<AddColumnOperation>()
                .Should()
                .ContainSingle(c => c.Name == "SelectedCourseId")
                .Subject;

            addColumn.Table.Should().Be("Students");
            addColumn.IsNullable.Should().BeTrue(
                "the column must be nullable so existing students stay valid and no data is rewritten");

            // A destructive operation anywhere in Up() would break the
            // "no destructive migration" requirement.
            var destructiveTypes = new[]
            {
                typeof(DropColumnOperation),
                typeof(DropTableOperation),
                typeof(RenameTableOperation)
            };
            builder.Operations.Should().NotContain(
                o => destructiveTypes.Contains(o.GetType()),
                "Up() must be strictly additive");
        }

        /// <summary>Up() creates the FK and index, and does not drop the course constraint.</summary>
        [Fact]
        public void Up_CreatesForeignKeyAndIndexForTheSelectedCourse()
        {
            var builder = RunAgainstMigratedMigration(up: true);

            builder.Operations.OfType<AddForeignKeyOperation>()
                .Should().ContainSingle(f =>
                    f.Table == "Students" &&
                    f.Columns.Single() == "SelectedCourseId" &&
                    f.PrincipalTable == "Courses");

            builder.Operations.OfType<CreateIndexOperation>()
                .Should().Contain(i =>
                    i.Table == "Students" && i.Name == "IX_Students_SelectedCourseId");
        }

        /// <summary>
        /// The backfill must only ever populate the new column, and only when
        /// the recovered course is unambiguous and in the student's own tenant.
        /// </summary>
        [Fact]
        public void Up_BackfillOnlyUpdatesTheNewColumnAndIsTenantScopedAndUnambiguous()
        {
            var sql = RunAgainstMigratedMigration(up: true).Operations
                .OfType<SqlOperation>()
                // Normalise line endings so the assertions are independent of
                // how the migration source file happens to be checked out.
                .Select(o => o.Sql.Replace("\r\n", "\n"))
                .ToList();

            foreach (var statement in sql)
            {
                // It only ever writes the new column.
                statement.Should().NotContain("DELETE ");
                statement.Should().NotContain("DROP ");
                statement.Should().NotContain("TRUNCATE ");
                var normalised = statement.TrimStart('\r', '\n');
                normalised.Should().StartWith("UPDATE \"Students\" s\nSET \"SelectedCourseId\" =",
                    "the backfill must only ever update Students.SelectedCourseId");

                // Ambiguity guards: a student enrolled in more than one course,
                // or in a programme with more than one active course, is left
                // NULL so the student re-selects rather than being silently
                // given the wrong course.
                statement.Should().Contain("\"SelectedCourseId\" IS NULL");

                // Tenant isolation: the recovered course must belong to the
                // student's own tenant, either via an EXISTS check or via a
                // per-student GROUP BY that is restricted to the tenant.
                statement.Should().Contain("tenant_id");

                // Idempotent: never re-writes a row that already has a value.
                statement.Should().Contain("\"is_deleted\" = false");
            }

            sql.Should().Contain(s => s.Contains("COUNT(DISTINCT \"CourseId\") = 1"));
            sql.Should().Contain(s => s.Contains("HAVING COUNT(*) = 1"));
        }

        /// <summary>
        /// The backfill picks a representative course id with
        /// <c>(array_agg(...))[1]</c>, not <c>MIN(...)</c>.
        ///
        /// <para>
        /// PostgreSQL has no <c>min(uuid)</c> aggregate, so <c>MIN("CourseId")</c>
        /// over a uuid column makes the migration fail at runtime with
        /// <c>42883: function min(uuid) does not exist</c>. The other guards here
        /// only inspect SQL text, so this one pins the construct explicitly - it
        /// was a real defect caught by running the migration against PostgreSQL.
        /// </para>
        /// </summary>
        [Fact]
        public void Up_BackfillDoesNotUseMinOnUuidColumns()
        {
            var sql = RunAgainstMigratedMigration(up: true).Operations
                .OfType<SqlOperation>()
                .Select(o => o.Sql.Replace("\r\n", "\n"))
                .ToList();

            foreach (var statement in sql)
            {
                statement.Should().NotContain("MIN(\"",
                    "PostgreSQL has no min() aggregate for uuid, which would fail the migration with 42883");

                // The unambiguous-value pick must use array_agg instead.
                statement.Should().Contain("(array_agg(",
                    "a single representative uuid must be selected with (array_agg(...))[1]");
            }
        }

        /// <summary>Down() fully reverses the migration.</summary>
        [Fact]
        public void Down_DropsTheColumnForeignKeyAndIndex()
        {
            var builder = RunAgainstMigratedMigration(up: false);

            builder.Operations.OfType<DropColumnOperation>()
                .Should().ContainSingle(c => c.Name == "SelectedCourseId" && c.Table == "Students");

            builder.Operations.OfType<DropForeignKeyOperation>()
                .Should().ContainSingle(f => f.Table == "Students");

            builder.Operations.OfType<DropIndexOperation>()
                .Should().Contain(i => i.Name == "IX_Students_SelectedCourseId");

            // Down() must not destroy student data.
            var destructiveTypes = new[]
            {
                typeof(DropTableOperation)
            };
            builder.Operations.Should().NotContain(
                o => destructiveTypes.Contains(o.GetType()),
                "Down() reverses the column, it must not drop student rows");
        }

        /// <summary>
        /// The model snapshot must have been regenerated, otherwise the API logs
        /// a PendingModelChangesWarning at startup.
        /// </summary>
        [Fact]
        public void ModelSnapshot_IsInSyncWithTheMigratedModel()
        {
            using var context = CreateDesignTimeContext();

            // HasPendingModelChanges() is exactly the check EF Core performs
            // before logging a PendingModelChangesWarning at startup. It is
            // false only when the snapshot was regenerated by the migration.
            context.Database.HasPendingModelChanges().Should().BeFalse(
                "the model snapshot must be regenerated by the migration or the API logs PendingModelChangesWarning at startup");

            // And the new property/relationship must be present on the model.
            var liveStudent = context.Model.FindEntityType(typeof(SMS.Domain.Entities.Student));
            liveStudent.Should().NotBeNull();
            liveStudent!.FindProperty("SelectedCourseId").Should().NotBeNull();
            liveStudent.GetForeignKeys()
                .Should().ContainSingle(fk => fk.Properties.Any(p => p.Name == "SelectedCourseId"));
        }
    }
}
