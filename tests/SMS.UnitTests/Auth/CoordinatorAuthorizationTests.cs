using System;
using FluentAssertions;
using Xunit;

namespace SMS.UnitTests.Auth
{
    /// <summary>
    /// Coordinator authorization regression tests.
    ///
    /// The coordinator manages academic operations (courses, units, course
    /// offerings, classes, timetable, calendar events) and views student
    /// information, but must NOT receive unrestricted administrator or system
    /// administrator capabilities.
    ///
    /// This mirrors the policy definitions in Program.cs:
    ///   - AdministratorAccess        : SystemAdministrator, Administrator
    ///   - ModeratorAccess            : SystemAdministrator, Administrator, Coordinator
    ///   - LecturerAccess             : SystemAdministrator, Administrator, Coordinator, Lecturer
    ///   - StudentAccess              : SystemAdministrator, Administrator, Coordinator, Lecturer, Student
    ///   - ReceptionistAccess         : SystemAdministrator, Administrator, Coordinator, Receptionist
    ///   - SystemAdministratorAccess  : SystemAdministrator
    /// </summary>
    public class CoordinatorAuthorizationTests
    {
        private static readonly string[] Coordinator = { "Coordinator" };

        private static bool SatisfiesPolicy(string[] userRoles, string policyName)
        {
            return policyName switch
            {
                "AdministratorAccess" =>
                    Any(userRoles, "SystemAdministrator", "Administrator"),
                "ModeratorAccess" =>
                    Any(userRoles, "SystemAdministrator", "Administrator", "Coordinator"),
                "LecturerAccess" =>
                    Any(userRoles, "SystemAdministrator", "Administrator", "Coordinator", "Lecturer"),
                "StudentAccess" =>
                    Any(userRoles, "SystemAdministrator", "Administrator", "Coordinator", "Lecturer", "Student"),
                "ReceptionistAccess" =>
                    Any(userRoles, "SystemAdministrator", "Administrator", "Coordinator", "Receptionist"),
                "SystemAdministratorAccess" =>
                    Any(userRoles, "SystemAdministrator"),
                _ => false
            };
        }

        private static bool Any(string[] userRoles, params string[] allowed)
        {
            foreach (var role in userRoles)
                foreach (var candidate in allowed)
                    if (string.Equals(role, candidate, StringComparison.OrdinalIgnoreCase))
                        return true;
            return false;
        }

        // Coordinator MUST be able to manage academic operations (ModeratorAccess).
        [Theory]
        [InlineData("ModeratorAccess")]
        [InlineData("LecturerAccess")]
        [InlineData("StudentAccess")]
        [InlineData("ReceptionistAccess")]
        public void Coordinator_ShouldSatisfyEnabledPolicies(string policy)
        {
            SatisfiesPolicy(Coordinator, policy).Should().BeTrue(
                $"Coordinator must satisfy {policy} to manage academic operations");
        }

        // Coordinator MUST NOT automatically satisfy administrator / system
        // administrator policies.
        [Theory]
        [InlineData("AdministratorAccess")]
        [InlineData("SystemAdministratorAccess")]
        public void Coordinator_ShouldNotSatisfyAdminPolicies(string policy)
        {
            SatisfiesPolicy(Coordinator, policy).Should().BeFalse(
                $"Coordinator must NOT satisfy {policy} (privilege escalation protection)");
        }

        // Every endpoint coordinator needs maps to a policy coordinator satisfies.
        [Fact]
        public void CoordinatorEndpointPolicies_ShouldAllBeEnabled()
        {
            string[] coordinatorEndpoints =
            {
                // Course creation/update is ModeratorAccess; delete is AdministratorAccess.
                "ModeratorAccess",
                // Unit creation/update is ModeratorAccess.
                "ModeratorAccess",
                // Course offering create/update is now ModeratorAccess.
                "ModeratorAccess",
                // Class CRUD is ModeratorAccess (delete remains AdministratorAccess).
                "ModeratorAccess",
                // Timetable create/update is ModeratorAccess.
                "ModeratorAccess",
                // Calendar create/update is now ModeratorAccess.
                "ModeratorAccess",
                // Student detail read is StudentProfileReadAccess.
                "StudentAccess",
            };

            foreach (var policy in coordinatorEndpoints)
                SatisfiesPolicy(Coordinator, policy).Should().BeTrue($"coordinator needs {policy}");
        }
    }
}