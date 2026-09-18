namespace SMS.Infrastructure.Options
{
    /// <summary>
    /// Binds the existing <c>RoadsDb</c> configuration section
    /// (compose: <c>RoadsDb__File</c>; env: ROADS_DB_FILE).
    /// The roads database is an EXTERNAL/OPAQUE dependency whose format, owner
    /// and schema are unknown (open requirement #19). Phase 2 only binds and
    /// validates the path — nothing in this codebase writes to it.
    /// </summary>
    public class RoadsDbOptions
    {
        public const string SectionName = "RoadsDb";

        /// <summary>Path of the external roads database file.</summary>
        public string File { get; set; } = "/app/data/roads.db";
    }
}
