namespace SMS.Application.Common
{
    /// <summary>
    /// Defines RBAC permission constants for the Accommodation module.
    /// These permissions are used to protect accommodation management endpoints.
    /// </summary>
    public static class AccommodationPermissions
    {
        /// <summary>View accommodation information (lanes, houses, dashboard)</summary>
        public const string View = "Accommodation.View";

        /// <summary>Create lanes and houses</summary>
        public const string Create = "Accommodation.Create";

        /// <summary>Edit lanes and houses</summary>
        public const string Edit = "Accommodation.Edit";

        /// <summary>Delete lanes and houses</summary>
        public const string Delete = "Accommodation.Delete";

        /// <summary>Assign students to houses</summary>
        public const string Assign = "Accommodation.Assign";

        /// <summary>Reassign students to different houses</summary>
        public const string Reassign = "Accommodation.Reassign";

        /// <summary>View and generate accommodation reports</summary>
        public const string Reports = "Accommodation.Reports";

        /// <summary>Manage house maintenance status</summary>
        public const string Maintenance = "Accommodation.Maintenance";

        /// <summary>Vacate houses</summary>
        public const string Vacate = "Accommodation.Vacate";

        /// <summary>All accommodation permissions for role assignment convenience</summary>
        public static readonly string[] All = new[]
        {
            View, Create, Edit, Delete, Assign, Reassign, Reports, Maintenance, Vacate
        };

        /// <summary>Permissions required for administrator-level access</summary>
        public static readonly string[] AdministratorPermissions = new[]
        {
            View, Create, Edit, Delete, Assign, Reassign, Reports, Maintenance, Vacate
        };

        /// <summary>Permissions required for receptionist-level access</summary>
        public static readonly string[] ReceptionistPermissions = new[]
        {
            View, Assign, Reassign, Reports, Maintenance, Vacate
        };
    }
}

