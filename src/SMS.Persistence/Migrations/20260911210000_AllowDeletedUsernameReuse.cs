using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SMS.Persistence.Data;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <summary>
    /// Allows a soft-deleted user's username to be reused by a newly created
    /// user. The Identity framework filters soft-deleted rows (IsDeleted = true)
    /// from every application query, so the username generator correctly reports
    /// a recycled username as "available", but the database UNIQUE index on
    /// AspNetUsers.NormalizedUserName (UserNameIndex) still covered the deleted
    /// row and produced a 23505 unique-violation -> HTTP 500 on POST /users for
    /// a user whose name matched a previously deleted account.
    /// Fix: recreate the index as a partial unique index over non-deleted rows.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260911210000_AllowDeletedUsernameReuse")]
    public partial class AllowDeletedUsernameReuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"UserNameIndex\";\n" +
                "CREATE UNIQUE INDEX \"UserNameIndex\" ON public.\"AspNetUsers\" (\"NormalizedUserName\") WHERE \"IsDeleted\" = false;\n" +
                "DROP INDEX IF EXISTS \"IX_AspNetUsers_Email\";\n" +
                "CREATE UNIQUE INDEX \"IX_AspNetUsers_Email\" ON public.\"AspNetUsers\" (\"Email\") WHERE \"IsDeleted\" = false;\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"UserNameIndex\";\n" +
                "CREATE UNIQUE INDEX \"UserNameIndex\" ON public.\"AspNetUsers\" (\"NormalizedUserName\");\n" +
                "DROP INDEX IF EXISTS \"IX_AspNetUsers_Email\";\n" +
                "CREATE UNIQUE INDEX \"IX_AspNetUsers_Email\" ON public.\"AspNetUsers\" (\"Email\");\n");
        }
    }
}