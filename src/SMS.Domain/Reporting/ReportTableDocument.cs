using System;
using System.Collections.Generic;

namespace SMS.Domain.Reporting
{
    /// <summary>A label/value pair describing a filter applied to a report.</summary>
    public class ReportFilterEntry
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// A generic tabular report document handed to the existing PDF/Excel
    /// generators (QuestPDF / EPPlus through SMS.Reporting). Keeps rendering
    /// concerns out of the report handlers while still producing branded,
    /// downloadable files.
    /// </summary>
    public class ReportTableDocument
    {
        /// <summary>System / school name shown in the report header.</summary>
        public string SystemName { get; set; } = "School Management System";

        public string ReportTitle { get; set; } = string.Empty;

        /// <summary>Who generated the report (username or email).</summary>
        public string GeneratedBy { get; set; } = string.Empty;

        /// <summary>UTC timestamp of report generation.</summary>
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

        public List<ReportFilterEntry> Filters { get; set; } = new List<ReportFilterEntry>();

        /// <summary>Summary totals rendered above the table.</summary>
        public List<string> SummaryLines { get; set; } = new List<string>();

        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>Rows of display-ready strings.</summary>
        public List<List<string>> Rows { get; set; } = new List<List<string>>();
    }
}
