using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMS.API.Extensions;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Guards the OMS Request dependency-injection registrations.
    ///
    /// <para>
    /// Production deployment found that <c>AddPersistenceServices</c> registered the
    /// OMS Order repositories but never the Phase 2C Request repositories. Because the
    /// Request MediatR handlers inject <c>IRequestRepository</c> and
    /// <c>IRequestTypeRepository</c> directly rather than through <c>IUnitOfWork</c>,
    /// nothing failed at build time and nothing failed in the unit suite, because those
    /// tests construct handlers with mocks. The gap only surfaced in production as
    /// <c>InvalidOperationException: Unable to resolve service for type
    /// 'SMS.Domain.Interfaces.IRequestRepository'</c>, returned to the client as
    /// HTTP 500 on every Request endpoint.
    /// </para>
    ///
    /// <para>
    /// This asserts the registrations exist so the same omission cannot be committed
    /// again. It inspects the <see cref="IServiceCollection"/> descriptors and therefore
    /// needs no live database.
    /// </para>
    /// </summary>
    public class OmsRequestServiceRegistrationTests
    {
        private static IServiceCollection BuildServices()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>(
                        "ConnectionStrings:DefaultConnection",
                        "Host=localhost;Database=unused;Username=u;Password=p")
                })
                .Build();

            var services = new ServiceCollection();
            services.AddPersistenceServices(configuration);
            return services;
        }

        [Theory]
        [InlineData(typeof(IRequestRepository))]
        [InlineData(typeof(IRequestTypeRepository))]
        [InlineData(typeof(IRequestCommentRepository))]
        [InlineData(typeof(IRequestNumberGenerator))]
        [InlineData(typeof(IOmsRequestNotifier))]
        public void AddPersistenceServices_RegistersEachOmsRequestService(Type serviceType)
        {
            var services = BuildServices();

            services.Should().Contain(
                d => d.ServiceType == serviceType,
                "{0} must be registered, or every OMS Request endpoint returns HTTP 500",
                serviceType.Name);
        }

        [Fact]
        public void AddPersistenceServices_StillRegistersTheOmsOrderRepositories()
        {
            // Guards the pre-existing Phase 2A registrations against regression when
            // the Request registrations were added next to them.
            var services = BuildServices();

            services.Should().Contain(d => d.ServiceType == typeof(IOrderRepository));
            services.Should().Contain(d => d.ServiceType == typeof(IRoadAccountRepository));
            services.Should().Contain(d => d.ServiceType == typeof(IOrderImportRepository));
        }
    }
}
