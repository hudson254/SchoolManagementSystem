using System;
using FluentAssertions;
using SMS.Domain.Notifications;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Covers the value rules enforced on every notification write.
    /// <para>
    /// These matter for security, not just tidiness:
    /// <c>NormalizeActionUrl</c> is what stops a notification being used as an
    /// open-redirect / <c>javascript:</c> vector, and the priority helpers are what
    /// keep the unread-badge EXISTS query and the UI's severity rendering in
    /// agreement with the persisted values.
    /// </para>
    /// </summary>
    public class NotificationCatalogTests
    {
        // ── Action URL safety ────────────────────────────────────────────────

        [Theory]
        [InlineData("/assignments/abc", "/assignments/abc")]
        [InlineData("/units", "/units")]
        [InlineData("/accommodation", "/accommodation")]
        [InlineData("/assignments/6f9619ff-8b86-d011-b42d-00c04fc964ff", "/assignments/6f9619ff-8b86-d011-b42d-00c04fc964ff")]
        public void NormalizeActionUrl_KeepsSafeRelativePaths(string input, string expected)
        {
            NotificationCatalog.NormalizeActionUrl(input).Should().Be(expected);
        }

        [Theory]
        // Absolute URLs: a notification must never point off-origin.
        [InlineData("https://evil.example/phish")]
        [InlineData("http://evil.example")]
        // Protocol-relative: browsers treat this as absolute.
        [InlineData("//evil.example/phish")]
        // Scheme-bearing: javascript: and data: are the XSS vectors.
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("/javascript:alert(1)")]
        // Backslashes are normalised to "/" by browsers, so "/\evil.example"
        // would otherwise slip past the protocol-relative check.
        [InlineData("/\\evil.example")]
        // Traversal.
        [InlineData("/../admin")]
        [InlineData("/a/../../b")]
        // Not root-relative at all.
        [InlineData("assignments/abc")]
        public void NormalizeActionUrl_RejectsUnsafeTargets(string input)
        {
            NotificationCatalog.NormalizeActionUrl(input).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeActionUrl_ReturnsNullForEmpty(string? input)
        {
            NotificationCatalog.NormalizeActionUrl(input).Should().BeNull();
        }

        [Fact]
        public void NormalizeActionUrl_TrimsAndCollapsesDuplicateSlashes()
        {
            NotificationCatalog.NormalizeActionUrl("  /assignments//abc  ")
                .Should().Be("/assignments/abc");
        }

        [Fact]
        public void NormalizeActionUrl_RejectsOverlongValue()
        {
            var tooLong = "/" + new string('a', NotificationCatalog.MaxActionUrlLength);
            NotificationCatalog.NormalizeActionUrl(tooLong).Should().BeNull();
        }

        [Fact]
        public void NormalizeActionUrl_RejectsControlCharacters()
        {
            NotificationCatalog.NormalizeActionUrl("/assignments/a\nb").Should().BeNull();
        }

        [Fact]
        public void NormalizeActionUrl_RejectsFragmentAndQuery()
        {
            // A fragment/query is unnecessary for a route target and is a common way
            // to smuggle surprising input past a naive validator.
            NotificationCatalog.NormalizeActionUrl("/assignments#frag").Should().BeNull();
            NotificationCatalog.NormalizeActionUrl("/assignments?x=1").Should().BeNull();
        }

        // ── Priority ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData("Informational", "Informational")]
        [InlineData("Normal", "Normal")]
        [InlineData("Important", "Important")]
        [InlineData("Critical", "Critical")]
        [InlineData("critical", "Critical")]
        [InlineData("  IMPORTANT  ", "Important")]
        public void NormalizePriority_MapsKnownValues(string input, string expected)
        {
            NotificationPriorities.Normalize(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Urgent")]
        [InlineData("SUPER-IMPORTANT")]
        public void NormalizePriority_FallsBackToNormal(string? input)
        {
            // Degrading to Normal (rather than throwing) means a legacy row can never
            // break the notification centre.
            NotificationPriorities.Normalize(input).Should().Be(NotificationPriorities.Normal);
        }

        [Fact]
        public void PriorityRank_IsOrderedBySeverity()
        {
            NotificationPriorities.Rank(NotificationPriorities.Informational).Should().BeLessThan(
                NotificationPriorities.Rank(NotificationPriorities.Normal));
            NotificationPriorities.Rank(NotificationPriorities.Normal).Should().BeLessThan(
                NotificationPriorities.Rank(NotificationPriorities.Important));
            NotificationPriorities.Rank(NotificationPriorities.Important).Should().BeLessThan(
                NotificationPriorities.Rank(NotificationPriorities.Critical));
        }

        [Fact]
        public void Priorities_AreExactlyTheFourDocumentedValues()
        {
            NotificationPriorities.All.Should().HaveCount(4);
            NotificationPriorities.All.Should().OnlyHaveUniqueItems();
        }

        // ── Types ────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("Accommodation", NotificationTypes.Accommodation)]
        [InlineData("accommodation", NotificationTypes.Accommodation)]
        [InlineData("Assignment", NotificationTypes.Assignment)]
        [InlineData("LectureNotes", NotificationTypes.LectureNotes)]
        [InlineData("Security", NotificationTypes.Security)]
        public void NormalizeType_MapsKnownValues(string input, string expected)
        {
            NotificationTypes.Normalize(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("SomethingUnmapped")]
        public void NormalizeType_FallsBackToSystem(string? input)
        {
            NotificationTypes.Normalize(input).Should().Be(NotificationTypes.System);
        }

        [Fact]
        public void Types_HaveNoDuplicates()
        {
            NotificationTypes.All.Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [InlineData(NotificationTypes.Security, NotificationPriorities.Critical)]
        [InlineData(NotificationTypes.Maintenance, NotificationPriorities.Critical)]
        [InlineData(NotificationTypes.AssignmentIssue, NotificationPriorities.Important)]
        [InlineData(NotificationTypes.AccountApproval, NotificationPriorities.Important)]
        [InlineData(NotificationTypes.Enrollment, NotificationPriorities.Informational)]
        [InlineData(NotificationTypes.Accommodation, NotificationPriorities.Normal)]
        public void DefaultPriorityFor_ClassifiesEachType(string type, string expected)
        {
            NotificationTypes.DefaultPriorityFor(type).Should().Be(expected);
        }

        [Fact]
        public void DefaultPriorityFor_AlwaysReturnsAKnownPriority()
        {
            foreach (var type in NotificationTypes.All)
            {
                NotificationPriorities.IsKnown(NotificationTypes.DefaultPriorityFor(type))
                    .Should().BeTrue($"type {type} must map to a known priority");
            }
        }

        // ── Title / message clamping ─────────────────────────────────────────

        [Fact]
        public void NormalizeTitle_ClampsToMaxLengthAndTrims()
        {
            var longTitle = new string('x', NotificationCatalog.MaxTitleLength + 500);
            NotificationCatalog.NormalizeTitle(longTitle)
                .Should().HaveLength(NotificationCatalog.MaxTitleLength);

            NotificationCatalog.NormalizeTitle("  spaced  ").Should().Be("spaced");
            NotificationCatalog.NormalizeTitle(null).Should().Be(string.Empty);
        }

        [Fact]
        public void NormalizeMessage_ClampsToMaxLengthAndTrims()
        {
            var longMessage = new string('y', NotificationCatalog.MaxMessageLength + 500);
            NotificationCatalog.NormalizeMessage(longMessage)
                .Should().HaveLength(NotificationCatalog.MaxMessageLength);

            NotificationCatalog.NormalizeMessage(null).Should().Be(string.Empty);
        }

        [Fact]
        public void ClampLimits_ArePositiveAndOrdered()
        {
            NotificationCatalog.MaxTitleLength.Should().BePositive();
            NotificationCatalog.MaxMessageLength.Should().BeGreaterThan(NotificationCatalog.MaxTitleLength);
            NotificationCatalog.MaxActionUrlLength.Should().BePositive();
        }
    }
}
