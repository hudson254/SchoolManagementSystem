using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SMS.API;
using SMS.Application.DTOs;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests
{
    public class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private string? _adminToken;
        private Guid? _firstDepartmentId;
        private Guid? _currentSemesterId;
        private Guid? _firstUnitId;

        public async Task InitializeAsync()
        {
            // Ensure database is created and seeded
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            // Get admin token
            _adminToken = await GetAuthTokenAsync("admin@school.com", "Admin123!");

            // Get first department ID
            _firstDepartmentId = await GetFirstDepartmentIdAsync();

            // Get current semester ID
            _currentSemesterId = await GetCurrentSemesterIdAsync();

            // Get first unit ID
            _firstUnitId = await GetFirstUnitIdAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }

        public async Task<string> GetAuthTokenAsync(string email, string password)
        {
            using var client = CreateClient();
            var loginRequest = new
            {
                email = email,
                password = password,
                rememberMe = true
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result?.AccessToken ?? string.Empty;
        }

        public async Task<Guid> GetFirstDepartmentIdAsync()
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

            var response = await client.GetAsync("/api/v1/departments");
            response.EnsureSuccessStatusCode();

            var departments = await response.Content.ReadFromJsonAsync<PagedResult<DepartmentDto>>();
            return departments?.Items.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        public async Task<Guid> GetCurrentSemesterIdAsync()
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

            var response = await client.GetAsync("/api/v1/semesters/current");
            response.EnsureSuccessStatusCode();

            var semester = await response.Content.ReadFromJsonAsync<SemesterDto>();
            return semester?.Id ?? Guid.Empty;
        }

        public async Task<Guid> GetFirstUnitIdAsync()
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

            var response = await client.GetAsync("/api/v1/units?page=1&pageSize=1");
            response.EnsureSuccessStatusCode();

            var units = await response.Content.ReadFromJsonAsync<PagedResult<UnitDto>>();
            return units?.Items.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        // DTOs for testing
        public class DepartmentDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        public class SemesterDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }
}