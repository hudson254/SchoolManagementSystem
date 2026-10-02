using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Routing and proxy-configuration invariants for the SignalR hub.
    /// <para>
    /// These guard two defects that only a live browser could observe, because in
    /// both cases the application and the proxy are each individually correct:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// nginx had only <c>location /hub/</c>. The canonical path <c>/hub</c> did not
    /// match it, fell through to the SPA <c>try_files $uri $uri/</c>, and nginx
    /// answered <c>301 -&gt; /hub/</c>. A WebSocket handshake cannot follow an HTTP
    /// redirect, so the socket never opened.
    /// </description></item>
    /// <item><description>
    /// nginx.conf was bind-mounted as a SINGLE FILE. <c>git checkout</c> replaces
    /// the file's inode, so a running container kept serving the previous
    /// configuration - a real stale-config condition during deployment.
    /// </description></item>
    /// </list>
    /// <para>
    /// These are configuration assertions rather than live HTTP tests because the
    /// failure mode is precisely "the route and the hub disagree", which only the
    /// rendered configuration can express.
    /// </para>
    /// </summary>
    public class NotificationHubRoutingTests
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
                $"Could not locate the repository root by walking up from '{AppContext.BaseDirectory}'.");
        }

        private static string ReadRepoFile(params string[] segments) =>
            File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()));

        /// <summary>Every nginx configuration that fronts the API.</summary>
        public static TheoryData<string> NginxConfigs => new()
        {
            Path.Combine("docker", "nginx", "nginx.conf"),
            Path.Combine("docker", "nginx-frontend.conf"),
        };

        private static string ProgramCs => ReadRepoFile("src", "SMS.API", "Program.cs");

        // ── Canonical endpoint ─────────────────────────────────────────────────

        [Fact]
        public void Hub_IsMappedAtTheCanonicalPathWithAuthorization()
        {
            // The client requests exactly "/hub"; the endpoint must match it.
            ProgramCs.Should().Contain("MapHub<NotificationHub>(\"/hub\")");

            // Authorization must be required explicitly on the endpoint as well
            // as via [Authorize] on the hub type.
            ProgramCs.Should().MatchRegex(
                @"MapHub<NotificationHub>\(\s*""/hub""\s*\)\s*\.RequireAuthorization\(\)");
        }

        [Fact]
        public void Hub_IsNotMappedAtTheTrailingSlashForm()
        {
            // A second "/hub/" mapping would create a second canonical endpoint
            // and re-introduce the ambiguity that produced the redirect.
            ProgramCs.Should().NotContain("MapHub<NotificationHub>(\"/hub/\")");
        }
        // ── nginx routing ──────────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_RoutesTheHubWithoutRequiringATrailingSlash(string relativePath)
        {
            var config = ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            // `^~` is what makes the bare "/hub" path match. Without it, the
            // prefix location "/hub/" would not match "/hub" at all.
            config.Should().MatchRegex(@"(?m)^\s*location\s+\^~\s+/hub\b");
        }

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_DoesNotRedirectTheHub(string relativePath)
        {
            var config = ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            // No `return 301`/`302`/`rewrite` may sit in the hub block: the
            // WebSocket upgrade cannot follow a redirect.
            var hubBlock = ExtractHubBlock(config);
            hubBlock.Should().NotMatch(@"(?m)^\s*(return\s+30[12]|rewrite\b)");
        }

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_ForwardsTheWebSocketUpgrade(string relativePath)
        {
            var hubBlock = ExtractHubBlock(ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar)));

            hubBlock.Should().Contain("proxy_http_version 1.1");
            hubBlock.Should().MatchRegex(@"proxy_set_header\s+Upgrade\s+\$http_upgrade");

            // Connection must come from the map, not a hard-coded "upgrade", so a
            // plain negotiate POST is not falsely advertised as an upgrade.
            hubBlock.Should().MatchRegex(@"proxy_set_header\s+Connection\s+\$connection_upgrade");
        }

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_KeepsTheHubConnectionAliveWhenIdle(string relativePath)
        {
            var hubBlock = ExtractHubBlock(ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar)));

            // A notification socket is idle by design; a 300s read timeout would
            // sever healthy connections. Buffering must also be off so pushes are
            // delivered immediately.
            hubBlock.Should().Contain("proxy_buffering off");
            var readTimeout = Regex.Match(hubBlock, @"proxy_read_timeout\s+(\d+)s");
            readTimeout.Success.Should().BeTrue("the hub block must set proxy_read_timeout");
            int.Parse(readTimeout.Groups[1].Value).Should().BeGreaterThanOrEqualTo(3600);
        }
        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_DoesNotScopeWebSocketProxyingBeyondTheHub(string relativePath)
        {
            var config = ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            // Broad WebSocket proxying on ordinary REST routes would be
            // unnecessary exposure; only the hub may upgrade.
            var hubBlock = ExtractHubBlock(config);
            var configWithoutHub = config.Replace(hubBlock, string.Empty);

            configWithoutHub.Should().NotMatch(
                @"proxy_set_header\s+Upgrade",
                "WebSocket forwarding must be scoped to the SignalR hub only");
        }

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_DeclaresTheConnectionUpgradeMap(string relativePath)
        {
            var config = ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            // $connection_upgrade is referenced by the hub block, so the map must
            // exist or nginx fails to start with an "unknown variable" error.
            config.Should().MatchRegex(@"map\s+\$http_upgrade\s+\$connection_upgrade\s*\{");
        }

        // ── Service-worker protection must survive the routing change ──────────

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_StillRevalidatesTheServiceWorker(string relativePath)
        {
            var config = ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            var swBlock = ExtractLocationBlock(config, "= /sw.js");
            swBlock.Should().NotBeNull("the service worker needs a dedicated location");
            swBlock!.Should().MatchRegex(@"Cache-Control\s+""[^""]*no-cache");

            // The worker must never inherit the immutable year-long asset cache,
            // which is what previously pinned browsers to one deployment.
            swBlock.Should().NotContain("immutable");
            swBlock.Should().NotContain("expires 1y");
        }

        [Theory]
        [MemberData(nameof(NginxConfigs))]
        public void Nginx_DoesNotServeTheServiceWorkerAsAnImmutableAsset(string relativePath)
        {
            var config = ReadRepoFile(relativePath.Split(Path.DirectorySeparatorChar));

            // `location = /sw.js` is an exact match and therefore always wins over
            // the regex asset block; assert the exact-match rule exists.
            config.Should().MatchRegex(@"(?m)^\s*location\s+=\s+/sw\.js\s*\{");
        }
        // ── Deployment safety: no single-file bind mount ───────────────────────

        [Theory]
        [InlineData("docker-compose.prod.yml")]
        [InlineData("docker-compose.yml")]
        [InlineData("docker-compose.dev.yml")]
        public void Compose_MountsTheNginxDirectoryNotASingleFile(string composeFile)
        {
            var compose = ReadRepoFile("docker", composeFile);

            // A single-file bind mount pins one inode for the container's lifetime;
            // `git checkout` replaces the inode, leaving nginx on the old config.
            compose.Should().NotMatch(
                @"(?m)^\s*-\s*\./nginx\.conf:/etc/nginx/nginx\.conf",
                "a single-file bind mount can silently serve a stale configuration");

            compose.Should().MatchRegex(
                @"(?m)^\s*-\s*\./nginx:/etc/nginx/\S+",
                "the nginx configuration directory must be mounted instead");
        }

        [Fact]
        public void Compose_StartsNginxWithTheMountedConfiguration()
        {
            // Without an explicit -c, the image would load its own
            // /etc/nginx/nginx.conf and ignore the mounted directory entirely.
            ReadRepoFile("docker", "docker-compose.prod.yml")
                .Should().MatchRegex(@"command:\s*\[\s*""nginx""\s*,\s*""-c""\s*,\s*""/etc/nginx/\S+nginx\.conf""");
        }

        [Fact]
        public void Compose_NoLongerMountsTheOldSingleFileConfig()
        {
            foreach (var composeFile in new[]
                     { "docker-compose.prod.yml", "docker-compose.yml", "docker-compose.dev.yml" })
            {
                // Only non-comment lines count: the compose files deliberately
                // DOCUMENT the old single-file mount in a comment to explain why
                // it was replaced, and reference the image's own config file in
                // another. Neither is an active mount.
                var activeLines = ReadRepoFile("docker", composeFile)
                    .Split('\n')
                    .Where(line => !line.TrimStart().StartsWith("#"))
                    .ToList();

                activeLines.Should().NotContain(
                    line => line.Contains("/etc/nginx/nginx.conf"),
                    $"{composeFile} still actively mounts the single-file configuration");
            }
        }

        [Fact]
        public void TheNginxConfigIsServedFromTheMountedDirectory()
        {
            // The file the compose file mounts must actually exist at that path.
            File.Exists(Path.Combine(
                FindRepositoryRoot(), "docker", "nginx", "nginx.conf"))
                .Should().BeTrue("docker/nginx/nginx.conf must exist for the directory mount");
        }

        // ── CSRF must still be enforced on negotiate ───────────────────────────

        [Fact]
        public void CsrfMiddleware_DoesNotExemptTheHubNegotiationEndpoint()
        {
            var middleware = ReadRepoFile(
                "src", "SMS.API", "Middleware", "CsrfProtectionMiddleware.cs");

            // Exempting /hub/negotiate would re-open CSRF on a cookie-authenticated
            // POST. The client sends the header instead.
            middleware.Should().NotContain("/hub");
            middleware.Should().NotContain("negotiate");
        }

        [Fact]
        public void CsrfMiddleware_KeepsTheExemptionsToAnonymousAuthEndpointsOnly()
        {
            var middleware = ReadRepoFile(
                "src", "SMS.API", "Middleware", "CsrfProtectionMiddleware.cs");

            var exempt = Regex.Match(
                middleware, @"CsrfExemptPaths\s*=\s*\{(?<body>[^}]*)\}", RegexOptions.Singleline);
            exempt.Success.Should().BeTrue("the exempt-path list must still exist");

            var entries = Regex.Matches(exempt.Groups["body"].Value, @"""(?<p>[^""]+)""")
                .Select(m => m.Groups["p"].Value)
                .ToList();

            entries.Should().NotBeEmpty("the anonymous auth endpoints must stay exempt");
            entries.Should().OnlyContain(p => p.StartsWith("/api/v1/auth/", StringComparison.Ordinal));
        }

        [Fact]
        public void CsrfMiddleware_StillValidatesTheHeaderAgainstTheCookie()
        {
            var middleware = ReadRepoFile(
                "src", "SMS.API", "Middleware", "CsrfProtectionMiddleware.cs");

            middleware.Should().Contain("\"XSRF-TOKEN\"");
            middleware.Should().Contain("\"X-CSRF-TOKEN\"");
            middleware.Should().Contain("Status403Forbidden");
            // Constant-time comparison must not be weakened.
            middleware.Should().Contain("CryptographicOperations.FixedTimeEquals");
        }

        // ── helpers ────────────────────────────────────────────────────────────

        /// <summary>Extracts the body of the `location ^~ /hub { ... }` block.</summary>
        private static string ExtractHubBlock(string config)
        {
            var match = Regex.Match(
                config,
                @"location\s+\^~\s+/hub\b[^{]*\{(?<body>.*?)\n\s*\}",
                RegexOptions.Singleline);

            match.Success.Should().BeTrue(
                "the configuration must contain a `location ^~ /hub` block");
            return match.Groups["body"].Value;
        }

        /// <summary>Extracts the body of an exact-match `location = &lt;path&gt;` block.</summary>
        private static string? ExtractLocationBlock(string config, string locationHeader)
        {
            var match = Regex.Match(
                config,
                @"location\s+" + Regex.Escape(locationHeader) + @"\s*\{(?<body>.*?)\n\s*\}",
                RegexOptions.Singleline);

            return match.Success ? match.Groups["body"].Value : null;
        }
    }
}