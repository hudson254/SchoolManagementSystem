namespace SMS.Infrastructure.Options
{
    /// <summary>
    /// Binds the existing <c>WALRecovery</c> configuration section
    /// (compose: <c>WALRecovery__Target</c>/<c>WALRecovery__RecoveryPath</c>;
    /// env: WAL_RECOVERY_TARGET / WAL_RECOVERY_PATH).
    /// Treat as an external-system contract (open requirement #20): Phase 2 only
    /// binds configuration and provides health/readiness information. No
    /// recovery behaviour is implemented or simulated.
    /// </summary>
    public class WalRecoveryOptions
    {
        public const string SectionName = "WALRecovery";

        /// <summary>Recovery target understood by the external system (default "latest").</summary>
        public string Target { get; set; } = "latest";

        /// <summary>Directory used by the external WAL recovery process.</summary>
        public string RecoveryPath { get; set; } = "/app/data/wal-recovery";
    }
}
