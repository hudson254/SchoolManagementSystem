using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Common;
using SMS.Infrastructure.Services;
using Xunit;

namespace SMS.UnitTests.Names
{
    public class NameParserTests
    {
        private readonly NameParser _parser;

        public NameParserTests()
        {
            var options = Options.Create(new TitleOptions
            {
                Titles = new List<TitleEntry>
                {
                    new() { Code = "Dr", DisplayText = "Dr.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Prof", DisplayText = "Prof.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Assoc. Prof.", DisplayText = "Assoc. Prof.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Asst. Prof.", DisplayText = "Asst. Prof.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Lecturer", DisplayText = "Lecturer", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Senior Lecturer", DisplayText = "Senior Lecturer", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Adjunct Lecturer", DisplayText = "Adjunct Lecturer", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Eng", DisplayText = "Eng.", Language = "en", Category = "Engineering", IsActive = true },
                    new() { Code = "Er", DisplayText = "Er.", Language = "en", Category = "Engineering", IsActive = true },
                    new() { Code = "Hon", DisplayText = "Hon.", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "Justice", DisplayText = "Justice", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "Judge", DisplayText = "Judge", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "Magistrate", DisplayText = "Magistrate", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "H.E.", DisplayText = "H.E.", Language = "en", Category = "Government", IsActive = true },
                    new() { Code = "Governor", DisplayText = "Governor", Language = "en", Category = "Government", IsActive = true },
                    new() { Code = "Senator", DisplayText = "Senator", Language = "en", Category = "Government", IsActive = true },
                    new() { Code = "MP", DisplayText = "MP", Language = "en", Category = "Government", IsActive = true },
                    new() { Code = "MCA", DisplayText = "MCA", Language = "en", Category = "Government", IsActive = true },
                    new() { Code = "Col", DisplayText = "Col.", Language = "en", Category = "Military", IsActive = true },
                    new() { Code = "Maj", DisplayText = "Maj.", Language = "en", Category = "Military", IsActive = true },
                    new() { Code = "Capt", DisplayText = "Capt.", Language = "en", Category = "Military", IsActive = true },
                    new() { Code = "Brig", DisplayText = "Brig.", Language = "en", Category = "Military", IsActive = true },
                    new() { Code = "Lt", DisplayText = "Lt.", Language = "en", Category = "Military", IsActive = true },
                    new() { Code = "Gen", DisplayText = "Gen.", Language = "en", Category = "Military", IsActive = true },
                    new() { Code = "Rev", DisplayText = "Rev.", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Fr", DisplayText = "Fr.", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Pastor", DisplayText = "Pastor", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Bishop", DisplayText = "Bishop", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Archbishop", DisplayText = "Archbishop", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Sheikh", DisplayText = "Sheikh", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Imam", DisplayText = "Imam", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "CPA", DisplayText = "CPA", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "CFA", DisplayText = "CFA", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "CISA", DisplayText = "CISA", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "CISSP", DisplayText = "CISSP", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "PMP", DisplayText = "PMP", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "PhD", DisplayText = "PhD", Language = "en", Category = "Academic", IsActive = true },
                }
            });

            var titleConfig = new TitleConfiguration(options);
            var logger = new Mock<ILogger<NameParser>>();
            _parser = new NameParser(titleConfig, logger.Object);
        }

        [Theory]
        [InlineData("Dr John Mwangi", "Dr", "John", "", "Mwangi")]
        [InlineData("Dr. John Mwangi", "Dr", "John", "", "Mwangi")]
        [InlineData("Prof. Jane Wambui", "Prof", "Jane", "", "Wambui")]
        [InlineData("Eng Peter Kariuki", "Eng", "Peter", "", "Kariuki")]
        [InlineData("Rev Samuel Maina", "Rev", "Samuel", "", "Maina")]
        [InlineData("Dr John Peter Mwangi", "Dr", "John", "Peter", "Mwangi")]
        [InlineData("John Mwangi", "", "John", "", "Mwangi")]
        [InlineData("John Peter Mwangi", "", "John", "Peter", "Mwangi")]
        [InlineData("DR JOHN MWANGI", "Dr", "John", "", "Mwangi")]
        [InlineData("dr john mwangi", "Dr", "John", "", "Mwangi")]
        [InlineData("DR. John Mwangi", "Dr", "John", "", "Mwangi")]
        [InlineData("Dr.  John   Mwangi", "Dr", "John", "", "Mwangi")]
        [InlineData("H.E. John Mwangi", "H.E.", "John", "", "Mwangi")]
        [InlineData("Hon. John Mwangi", "Hon", "John", "", "Mwangi")]
        [InlineData("CPA John Mwangi", "CPA", "John", "", "Mwangi")]
        [InlineData("PhD John Mwangi", "PhD", "John", "", "Mwangi")]
        [InlineData("Assoc. Prof. John Mwangi", "Assoc. Prof.", "John", "", "Mwangi")]
        [InlineData("Asst. Prof. John Mwangi", "Asst. Prof.", "John", "", "Mwangi")]
        [InlineData("Senior Lecturer John Mwangi", "Senior Lecturer", "John", "", "Mwangi")]
        [InlineData("Adjunct Lecturer John Mwangi", "Adjunct Lecturer", "John", "", "Mwangi")]
        [InlineData("Lecturer John Mwangi", "Lecturer", "John", "", "Mwangi")]
        [InlineData("Er. John Mwangi", "Er", "John", "", "Mwangi")]
        [InlineData("Fr. John Mwangi", "Fr", "John", "", "Mwangi")]
        [InlineData("Pastor John Mwangi", "Pastor", "John", "", "Mwangi")]
        [InlineData("Bishop John Mwangi", "Bishop", "John", "", "Mwangi")]
        [InlineData("Archbishop John Mwangi", "Archbishop", "John", "", "Mwangi")]
        [InlineData("Sheikh John Mwangi", "Sheikh", "John", "", "Mwangi")]
        [InlineData("Imam John Mwangi", "Imam", "John", "", "Mwangi")]
        [InlineData("Governor John Mwangi", "Governor", "John", "", "Mwangi")]
        [InlineData("Senator John Mwangi", "Senator", "John", "", "Mwangi")]
        [InlineData("MP John Mwangi", "MP", "John", "", "Mwangi")]
        [InlineData("MCA John Mwangi", "MCA", "John", "", "Mwangi")]
        [InlineData("Col. John Mwangi", "Col", "John", "", "Mwangi")]
        [InlineData("Maj. John Mwangi", "Maj", "John", "", "Mwangi")]
        [InlineData("Capt. John Mwangi", "Capt", "John", "", "Mwangi")]
        [InlineData("Brig. John Mwangi", "Brig", "John", "", "Mwangi")]
        [InlineData("Lt. John Mwangi", "Lt", "John", "", "Mwangi")]
        [InlineData("Gen. John Mwangi", "Gen", "John", "", "Mwangi")]
        [InlineData("Judge John Mwangi", "Judge", "John", "", "Mwangi")]
        [InlineData("Justice John Mwangi", "Justice", "John", "", "Mwangi")]
        [InlineData("Magistrate John Mwangi", "Magistrate", "John", "", "Mwangi")]
        [InlineData("CFA John Mwangi", "CFA", "John", "", "Mwangi")]
        [InlineData("CISA John Mwangi", "CISA", "John", "", "Mwangi")]
        [InlineData("CISSP John Mwangi", "CISSP", "John", "", "Mwangi")]
        [InlineData("PMP John Mwangi", "PMP", "John", "", "Mwangi")]
        public void ParseName_ExtractsTitleAndNameParts(string input, string expectedTitle, string expectedFirst, string expectedMiddle, string expectedLast)
        {
            var result = _parser.ParseName(input);

            result.Title.Should().Be(expectedTitle);
            result.FirstName.Should().Be(expectedFirst);
            result.MiddleName.Should().Be(expectedMiddle);
            result.LastName.Should().Be(expectedLast);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Dr Dr John Mwangi")]
        [InlineData("Prof Prof Jane Wambui")]
        [InlineData("Dr Prof Eng John Mwangi")]
        public void ParseName_RejectsMultipleTitles(string input)
        {
            var result = _parser.ParseName(input);

            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("multiple");
        }

        [Theory]
        [InlineData("DR", "Dr")]
        [InlineData("dr", "Dr")]
        [InlineData("DR.", "Dr")]
        [InlineData("Dr", "Dr")]
        [InlineData("DR. ", "Dr")]
        public void ParseName_NormalizesTitleCase(string input, string expectedTitle)
        {
            var result = _parser.ParseName($"{input} John Mwangi");

            result.Title.Should().Be(expectedTitle);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Dr.  John   Mwangi")]
        [InlineData("  Dr John Mwangi  ")]
        [InlineData("Dr   John   Mwangi")]
        public void ParseName_TrimsExtraSpaces(string input)
        {
            var result = _parser.ParseName(input);

            result.FirstName.Should().Be("John");
            result.LastName.Should().Be("Mwangi");
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Dr.. John Mwangi")]
        [InlineData("Dr. John Mwangi")]
        public void ParseName_HandlesDuplicatePeriods(string input)
        {
            var result = _parser.ParseName(input);

            result.Title.Should().Be("Dr");
            result.FirstName.Should().Be("John");
            result.LastName.Should().Be("Mwangi");
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("José María García")]
        [InlineData("Müller")]
        [InlineData("Sørensen")]
        [InlineData("Żółć")]
        public void ParseName_HandlesUnicodeCharacters(string input)
        {
            var result = _parser.ParseName(input);

            result.IsValid.Should().BeTrue();
            result.FirstName.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData("Dr. John Mwangi", "Dr")]
        [InlineData("Prof. Jane Wambui", "Prof")]
        [InlineData("John Mwangi", "")]
        public void ParseName_GeneratesDisplayNameWithTitle(string input, string expectedTitle)
        {
            var result = _parser.ParseName(input);

            if (string.IsNullOrEmpty(expectedTitle))
            {
                result.DisplayName.Should().Be($"{result.FirstName} {result.LastName}".Trim());
            }
            else
            {
                result.DisplayName.Should().Be($"{result.TitleDisplayText} {result.FirstName} {result.LastName}".Trim());
            }
        }

        [Fact]
        public void ParseName_UnknownTitleTreatedAsFirstName()
        {
            var result = _parser.ParseName("XYZ John Mwangi");

            result.Title.Should().BeEmpty();
            result.FirstName.Should().Be("XYZ");
            result.LastName.Should().Be("Mwangi");
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ParseName_EmptyInputReturnsInvalid()
        {
            var result = _parser.ParseName("");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void ParseName_SingleNameReturnsInvalid()
        {
            var result = _parser.ParseName("John");

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void ParseName_NormalizesDisplayText()
        {
            var result = _parser.ParseName("Dr John Mwangi");

            result.Title.Should().Be("Dr");
            result.TitleDisplayText.Should().Be("Dr.");
        }

        [Fact]
        public void ParseName_GenerateSortKeyIgnoresTitle()
        {
            var key1 = _parser.GenerateSortKey("Dr. John Mwangi");
            var key2 = _parser.GenerateSortKey("John Mwangi");

            key1.Should().Be(key2);
        }

        [Fact]
        public void ParseName_SanitizeForFileNameRemovesTitle()
        {
            var result = _parser.SanitizeForFileName("Dr. John Mwangi");

            result.Should().NotContain("dr");
            result.Should().Be("johnmwangi");
        }

        [Fact]
        public void ParseName_GenerateDisplayName()
        {
            var result = _parser.GenerateDisplayName("Dr", "John", "Peter", "Mwangi");

            result.Should().Be("Dr. John Peter Mwangi");
        }

        [Fact]
        public void ParseName_GenerateDisplayNameWithoutTitle()
        {
            var result = _parser.GenerateDisplayName(null, "John", null, "Mwangi");

            result.Should().Be("John Mwangi");
        }
    }
}
