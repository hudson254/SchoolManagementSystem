using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Services;
using Xunit;

namespace SMS.UnitTests.Names
{
    public class UsernameGeneratorTests
    {
        private readonly UsernameGenerator _generator;
        private readonly Mock<IUserManagerService> _mockUserManager;

        public UsernameGeneratorTests()
        {
            var options = Options.Create(new TitleOptions
            {
                Titles = new List<TitleEntry>
                {
                    new() { Code = "Dr", DisplayText = "Dr.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Prof", DisplayText = "Prof.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Eng", DisplayText = "Eng.", Language = "en", Category = "Engineering", IsActive = true },
                    new() { Code = "Rev", DisplayText = "Rev.", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Hon", DisplayText = "Hon.", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "H.E.", DisplayText = "H.E.", Language = "en", Category = "Government", IsActive = true },
                    new() { Code = "CPA", DisplayText = "CPA", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "PhD", DisplayText = "PhD", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Assoc. Prof.", DisplayText = "Assoc. Prof.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Asst. Prof.", DisplayText = "Asst. Prof.", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Senior Lecturer", DisplayText = "Senior Lecturer", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Adjunct Lecturer", DisplayText = "Adjunct Lecturer", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Lecturer", DisplayText = "Lecturer", Language = "en", Category = "Academic", IsActive = true },
                    new() { Code = "Er", DisplayText = "Er.", Language = "en", Category = "Engineering", IsActive = true },
                    new() { Code = "Fr", DisplayText = "Fr.", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Pastor", DisplayText = "Pastor", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Bishop", DisplayText = "Bishop", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Archbishop", DisplayText = "Archbishop", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Sheikh", DisplayText = "Sheikh", Language = "en", Category = "Religious", IsActive = true },
                    new() { Code = "Imam", DisplayText = "Imam", Language = "en", Category = "Religious", IsActive = true },
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
                    new() { Code = "Judge", DisplayText = "Judge", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "Justice", DisplayText = "Justice", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "Magistrate", DisplayText = "Magistrate", Language = "en", Category = "Legal", IsActive = true },
                    new() { Code = "CFA", DisplayText = "CFA", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "CISA", DisplayText = "CISA", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "CISSP", DisplayText = "CISSP", Language = "en", Category = "Professional", IsActive = true },
                    new() { Code = "PMP", DisplayText = "PMP", Language = "en", Category = "Professional", IsActive = true },
                }
            });

            var titleConfig = new TitleConfiguration(options);
            var logger = new Mock<ILogger<NameParser>>();
            var nameParser = new NameParser(titleConfig, logger.Object);

            _mockUserManager = new Mock<IUserManagerService>();
            _mockUserManager.Setup(x => x.FindByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            _generator = new UsernameGenerator(_mockUserManager.Object, nameParser);
        }

        [Theory]
        [InlineData("Dr", "John", "Mwangi", "john.mwangi")]
        [InlineData("Prof", "Jane", "Wambui", "jane.wambui")]
        [InlineData("", "John", "Mwangi", "john.mwangi")]
        [InlineData("Eng", "Peter", "Kariuki", "peter.kariuki")]
        [InlineData("Rev", "Samuel", "Maina", "samuel.maina")]
        [InlineData("H.E.", "John", "Mwangi", "john.mwangi")]
        [InlineData("Hon", "John", "Mwangi", "john.mwangi")]
        [InlineData("CPA", "John", "Mwangi", "john.mwangi")]
        [InlineData("PhD", "John", "Mwangi", "john.mwangi")]
        [InlineData("Assoc. Prof.", "John", "Mwangi", "john.mwangi")]
        [InlineData("Asst. Prof.", "John", "Mwangi", "john.mwangi")]
        [InlineData("Senior Lecturer", "John", "Mwangi", "john.mwangi")]
        [InlineData("Adjunct Lecturer", "John", "Mwangi", "john.mwangi")]
        [InlineData("Lecturer", "John", "Mwangi", "john.mwangi")]
        [InlineData("Er", "John", "Mwangi", "john.mwangi")]
        [InlineData("Fr", "John", "Mwangi", "john.mwangi")]
        [InlineData("Pastor", "John", "Mwangi", "john.mwangi")]
        [InlineData("Bishop", "John", "Mwangi", "john.mwangi")]
        [InlineData("Archbishop", "John", "Mwangi", "john.mwangi")]
        [InlineData("Sheikh", "John", "Mwangi", "john.mwangi")]
        [InlineData("Imam", "John", "Mwangi", "john.mwangi")]
        [InlineData("Governor", "John", "Mwangi", "john.mwangi")]
        [InlineData("Senator", "John", "Mwangi", "john.mwangi")]
        [InlineData("MP", "John", "Mwangi", "john.mwangi")]
        [InlineData("MCA", "John", "Mwangi", "john.mwangi")]
        [InlineData("Col", "John", "Mwangi", "john.mwangi")]
        [InlineData("Maj", "John", "Mwangi", "john.mwangi")]
        [InlineData("Capt", "John", "Mwangi", "john.mwangi")]
        [InlineData("Brig", "John", "Mwangi", "john.mwangi")]
        [InlineData("Lt", "John", "Mwangi", "john.mwangi")]
        [InlineData("Gen", "John", "Mwangi", "john.mwangi")]
        [InlineData("Judge", "John", "Mwangi", "john.mwangi")]
        [InlineData("Justice", "John", "Mwangi", "john.mwangi")]
        [InlineData("Magistrate", "John", "Mwangi", "john.mwangi")]
        [InlineData("CFA", "John", "Mwangi", "john.mwangi")]
        [InlineData("CISA", "John", "Mwangi", "john.mwangi")]
        [InlineData("CISSP", "John", "Mwangi", "john.mwangi")]
        [InlineData("PMP", "John", "Mwangi", "john.mwangi")]
        public async Task GenerateUsername_StripsTitles(string title, string firstName, string lastName, string expectedUsername)
        {
            var result = await _generator.GenerateUsernameAsync(firstName, lastName);

            result.Should().Be(expectedUsername);
        }

        [Theory]
        [InlineData("Dr", "John", "Mwangi", "john.mwangi")]
        [InlineData("Prof", "Jane", "Wambui", "jane.wambui")]
        [InlineData("", "John", "Mwangi", "john.mwangi")]
        public async Task GenerateUsername_NeverIncludesTitleInUsername(string title, string firstName, string lastName, string expectedUsername)
        {
            var result = await _generator.GenerateUsernameAsync(firstName, lastName);

            result.Should().NotContain("dr");
            result.Should().NotContain("prof");
            result.Should().NotContain("eng");
            result.Should().NotContain("rev");
            result.Should().NotContain("hon");
            result.Should().NotContain("he");
            result.Should().NotContain("cpa");
            result.Should().NotContain("phd");
            result.Should().Be(expectedUsername);
        }

        [Theory]
        [InlineData("DR", "John", "Mwangi", "john.mwangi")]
        [InlineData("dr", "John", "Mwangi", "john.mwangi")]
        [InlineData("DR.", "John", "Mwangi", "john.mwangi")]
        [InlineData("Dr", "John", "Mwangi", "john.mwangi")]
        public async Task GenerateUsername_NormalizesTitleCase(string title, string firstName, string lastName, string expectedUsername)
        {
            var result = await _generator.GenerateUsernameAsync(firstName, lastName);

            result.Should().Be(expectedUsername);
        }

        [Theory]
        [InlineData("José", "García", "josé.garcía")]
        [InlineData("Müller", "Sørensen", "müller.sørensen")]
        public async Task GenerateUsername_HandlesUnicodeCharacters(string firstName, string lastName, string expectedUsername)
        {
            var result = await _generator.GenerateUsernameAsync(firstName, lastName);

            result.Should().Be(expectedUsername);
        }

        [Fact]
        public async Task GenerateUsername_EmptyFirstName()
        {
            var result = await _generator.GenerateUsernameAsync("", "Mwangi");

            result.Should().Be("mwangi");
        }

        [Fact]
        public async Task GenerateUsername_EmptyLastName()
        {
            var result = await _generator.GenerateUsernameAsync("John", "");

            result.Should().Be("john");
        }

        [Fact]
        public async Task GenerateUsername_TrimsWhitespace()
        {
            var result = await _generator.GenerateUsernameAsync("  John  ", "  Mwangi  ");

            result.Should().Be("john.mwangi");
        }

        [Fact]
        public async Task GenerateUsername_Lowercases()
        {
            var result = await _generator.GenerateUsernameAsync("JOHN", "MWANGI");

            result.Should().Be("john.mwangi");
        }

        [Fact]
        public async Task GenerateUsername_RemovesSpecialCharacters()
        {
            var result = await _generator.GenerateUsernameAsync("John!", "Mwangi@");

            result.Should().Be("john.mwangi");
        }

        [Fact]
        public async Task GenerateUsername_CollisionAppendsNumber()
        {
            _mockUserManager.SetupSequence(x => x.FindByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(new User())  // john.mwangi is taken
                .ReturnsAsync((User)null);  // john.mwangi2 is available

            var result = await _generator.GenerateUsernameAsync("John", "Mwangi");

            result.Should().Be("john.mwangi2");
        }

        [Fact]
        public async Task GenerateUsernameFromFullName_StripsTitle()
        {
            _mockUserManager.Setup(x => x.FindByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var result = await _generator.GenerateUsernameFromFullNameAsync("Dr. John Mwangi");

            result.Should().Be("john.mwangi");
            result.Should().NotContain("dr");
        }

        [Fact]
        public async Task GenerateUsernameFromFullName_ProfTitle()
        {
            _mockUserManager.Setup(x => x.FindByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var result = await _generator.GenerateUsernameFromFullNameAsync("Prof. Jane Wambui");

            result.Should().Be("jane.wambui");
            result.Should().NotContain("prof");
        }
    }
}
