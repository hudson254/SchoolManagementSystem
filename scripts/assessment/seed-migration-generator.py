# -*- coding: utf-8 -*-
import sys, os, io
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from seed_sql import *
def cs_sql(indent, sql):
    """Convert a multi-line SQL string into a C# concatenated-string expression.

    Idempotent escaping: remove any existing backslashes, then escape every
    double quote exactly once as \\" so the runtime SQL keeps real quotes.
    Emit \\n between lines so the runtime SQL keeps real newlines.
    """
    pad = " " * indent
    lines = sql.split("\n")
    parts = []
    n = len(lines)
    for i, ln in enumerate(lines):
        if ln == "":
            continue
        esc = __import__("re").sub(r"\\+", "", ln).replace('"', '\\"')
        if i < n - 1:
            parts.append(pad + '"' + esc + '\\n"')
        else:
            parts.append(pad + '"' + esc + '"')
    return "\n" + (" +\n".join(parts))

head = """using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SMS.Persistence.Data;

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
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260908120000_SeedAssessmentGradingData")]
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
    "            migrationBuilder.Sql(" + cs_sql(16, TYPES_SQL) + ");\n"
    "\n"
    "            // Default grading scale with 4 bands.\n"
    "            migrationBuilder.Sql(" + cs_sql(16, SCALES_SQL + BANDS_SQL) + ");\n"
    "\n"
    "            // Default certificate eligibility rule.\n"
    "            migrationBuilder.Sql(" + cs_sql(16, RULES_SQL) + ");\n"
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
