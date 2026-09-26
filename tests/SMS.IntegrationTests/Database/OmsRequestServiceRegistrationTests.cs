using FluentAssertions;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Guards the OMS Request dependency-injection registrations in the real
    /// composition root.
    ///
    /// <para>
    /// Production deployment returned HTTP 500 on every Request endpoint:
    /// <c>Unable to resolve service for type 'SMS.Domain.Interfaces.IRequestRepository'</c>.
    /// <c>AddPersistenceServices</c> in <c>Extensions/ServiceExtensions.cs</c> does
    /// register the Request repositories, but <c>Program.cs</c> never calls that
    /// extension - it wires services inline. Registering them in the unused extension
    /// therefore changed nothing, which is exactly how this gap survived review.
    /// </para>
    ///
    /// <para>
    /// Unit tests cannot catch it: handlers are constructed with mocks, and nothing
    /// resolves them from a container.
    /// </para>
    ///
    /// <para>
    /// This asserts the registrations are present in <c>src/SMS.API/Program.cs</c>,
    /// which is the code that actually builds the container.
    /// </para>
    /// </summary>
    public class OmsRequestServiceRegistrationTests
    {
        /// <summary>Locates the API composition root by walking up to the repository root.</summary>
        private static string LocateProgramCs()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "SMS.API", "Program.cs");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException(
                "Could not locate src/SMS.API/Program.cs from " + AppContext.BaseDirectory);
        }

        private static string ReadProgramCs() => File.ReadAllText(LocateProgramCs());

        [Theory]
        [InlineData("IRequestRepository")]
        [InlineData("IRequestTypeRepository")]
        [InlineData("IRequestCommentRepository")]
        [InlineData("IRequestNumberGenerator")]
        [InlineData("IOmsRequestNotifier")]
        public void Program_RegistersOmsRequestService(string serviceType)
        {
            var source = ReadProgramCs();

            source.Should().Contain(
                $"AddScoped<{serviceType}",
                "{0} must be registered in Program.cs, or every OMS Request endpoint " +
                "returns HTTP 500. Note that AddPersistenceServices() is not invoked " +
                "by Program.cs, so registering it there has no effect.",
                serviceType);
        }

        [Fact]
        public void Program_StillRegistersTheOmsOrderRepository()
        {
            // Guards the pre-existing Phase 2A registration against regression.
            ReadProgramCs().Should().Contain("AddScoped<IOrderRepository,");
        }
    }
}
