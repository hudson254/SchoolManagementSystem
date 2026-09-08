# -*- coding: utf-8 -*-
import io
import gen_data as G

def cs_sql(indent, sql):
    """Convert a multi-line SQL string into a C# concatenated-string expression.
    Double quotes inside are escaped as \\\" (valid in normal C# strings)."""
    pad = " " * indent
    lines = sql.split("\n")
    parts = []
    for i, ln in enumerate(lines):
        if ln == "":
            continue
        escaped = ln.replace("\\\"", "\\\"")
        suffix = "\\n\"" if i < len(lines) - 1 else "\""
        parts.append(pad + "\"" + escaped + suffix)
    return "\n" + (" +\n".join(parts))

head = """using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <summary>
    /// Seeds the configurable assessment / grading data required by the
    /// centralized Assessment Engine and adds moderation record metadata.
    /// Idempotent: rows are only inserted when absent per tenant. No existing
    /// rows are modified or deleted. Inserts are aligned to the resolved
    /// tenant so PostgreSQL Row Level Security is honoured.
    /// </summary>
    public partial class SeedAssessmentGradingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid?>(
                name: "StudentId",
                table: "ModerationRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid?>(
                name: "MarkId",
                table: "ModerationRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal?>(
                name: "OriginalScore",
                table: "ModerationRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal?>(
                name: "RevisedScore",
                table: "ModerationRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string?>(
                name: "ReviewerComments",
                table: "ModerationRecords",
                type: "text",
                nullable: true);
"""

body = (
    "            // 13 institutional assessment types (configurable; admins may add more).\n"
    "            migrationBuilder.Sql(" + cs_sql(16, G.TYPES_SQL) + ");\n"
    "\n"
    "            // Default grading scale with 4 bands.\n"
    "            migrationBuilder.Sql(" + cs_sql(16, G.SCALES_SQL + G.BANDS_SQL) + ");\n"
    "\n"
    "            // Default certificate eligibility rule.\n"
    "            migrationBuilder.Sql(" + cs_sql(16, G.RULES_SQL) + ");\n"
)

tail = """        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReviewerComments", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "RevisedScore", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "OriginalScore", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "MarkId", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "StudentId", table: "ModerationRecords");
        }
    }
}
"""

with io.open("src/SMS.Persistence/Migrations/20260908120000_SeedAssessmentGradingData.cs", "w", encoding="utf-8", newline="") as f:
    f.write(head + body + tail)
print("Migration rewritten, chars =", len(head + body + tail))