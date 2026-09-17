using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace SMS.UnitTests.Configuration
{
    /// <summary>
    /// Guards against the production startup failure that was caused by a
    /// malformed <c>appsettings.Production.json</c>.
    ///
    /// ASP.NET Core loads <c>appsettings.{Environment}.json</c> at startup and
    /// throws as soon as the file cannot be parsed, even though the file is
    /// registered as optional (optional only means "missing is allowed", not
    /// "invalid is allowed"):
    ///
    /// <code>
    /// Unhandled exception. System.IO.InvalidDataException: Failed to load configuration
    /// from file 'appsettings.Production.json'.
    ///  ---&gt; System.FormatException: Could not parse the JSON file.
    /// </code>
    ///
    /// The process exits before it ever listens, so the failure looks like the
    /// container "never starts" / "waits forever". These tests parse every
    /// appsettings file that ships with the API so the defect is caught in CI
    /// instead of in production.
    /// </summary>
    public class AppSettingsJsonValidityTests
    {
        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SchoolManagementSystem.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Could not locate the repository root (a directory containing SchoolManagementSystem.sln) " +
                $"by walking up from '{AppContext.BaseDirectory}'.");
        }

        private static string ApiProjectDirectory =>
            Path.Combine(FindRepositoryRoot(), "src", "SMS.API");

        [Fact]
        public void ApiProject_ShipsAtLeastOneAppSettingsFile()
        {
            var files = Directory.GetFiles(ApiProjectDirectory, "appsettings*.json");
            Assert.NotEmpty(files);
        }

        [Theory]
        [InlineData("appsettings.json")]
        [InlineData("appsettings.Production.json")]
        [InlineData("appsettings.Staging.json")]
        [InlineData("appsettings.Test.json")]
        [InlineData("appsettings.Testing.json")]
        [InlineData("appsettings.Development.json")]
        public void AppSettingsFile_IsValidJson_WhenPresent(string fileName)
        {
            var path = Path.Combine(ApiProjectDirectory, fileName);

            // Environment-specific files are legitimately optional (e.g.
            // appsettings.Production.json is gitignored and supplied per
            // deployment), but when the file IS present it must parse, because
            // an unparseable file aborts application startup.
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path);

            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false
            });

            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        }

        [Fact]
        public void AllShippedAppSettingsFiles_AreValidJson()
        {
            var invalid = Directory
                .GetFiles(ApiProjectDirectory, "appsettings*.json")
                .Where(file =>
                {
                    try
                    {
                        using var _ = JsonDocument.Parse(File.ReadAllText(file));
                        return false;
                    }
                    catch (JsonException)
                    {
                        return true;
                    }
                })
                .Select(Path.GetFileName)
                .ToList();

            Assert.True(
                invalid.Count == 0,
                $"These appsettings files are not valid JSON and would abort application startup: {string.Join(", ", invalid)}");
        }
    }
}
