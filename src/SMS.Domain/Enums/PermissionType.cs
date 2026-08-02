namespace SMS.Domain.Enums
{
    /// <summary>
    /// Permission types for Role-Based Access Control
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// Create permission
        /// </summary>
        Create = 1,

        /// <summary>
        /// Read permission
        /// </summary>
        Read = 2,

        /// <summary>
        /// Update permission
        /// </summary>
        Update = 3,

        /// <summary>
        /// Delete permission
        /// </summary>
        Delete = 4,

        /// <summary>
        /// Full control (all permissions)
        /// </summary>
        FullControl = 5,

        /// <summary>
        /// Approve permission
        /// </summary>
        Approve = 6,

        /// <summary>
        /// Execute permission
        /// </summary>
        Execute = 7,

        /// <summary>
        /// Export permission
        /// </summary>
        Export = 8,

        /// <summary>
        /// Import permission
        /// </summary>
        Import = 9
    }
}