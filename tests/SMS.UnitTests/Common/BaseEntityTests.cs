using System;
using FluentAssertions;
using SMS.Domain.Common;
using Xunit;

namespace SMS.UnitTests.Common
{
    public class BaseEntityTests
    {
        private class TestEntity : BaseEntity
        {
        }

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            entity.Id.Should().NotBeEmpty();
            entity.TenantId.Should().Be(Guid.Empty);
            entity.CreatedBy.Should().BeNull();
            entity.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            entity.ModifiedBy.Should().BeNull();
            entity.ModifiedDate.Should().BeNull();
            entity.DeletedBy.Should().BeNull();
            entity.DeletedDate.Should().BeNull();
            entity.IsDeleted.Should().BeFalse();
            entity.RowVersion.Should().BeNull();
        }

        [Fact]
        public void SoftDelete_ShouldMarkEntityAsDeleted()
        {
            // Arrange
            var entity = new TestEntity();
            var deletedBy = "TestUser";

            // Act
            entity.SoftDelete(deletedBy);

            // Assert
            entity.IsDeleted.Should().BeTrue();
            entity.DeletedBy.Should().Be(deletedBy);
            entity.DeletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Restore_ShouldRestoreDeletedEntity()
        {
            // Arrange
            var entity = new TestEntity();
            entity.SoftDelete("TestUser");

            // Act
            entity.Restore();

            // Assert
            entity.IsDeleted.Should().BeFalse();
            entity.DeletedBy.Should().BeNull();
            entity.DeletedDate.Should().BeNull();
        }

        [Fact]
        public void SoftDelete_Twice_ShouldUpdateDeletedDate()
        {
            // Arrange
            var entity = new TestEntity();
            entity.SoftDelete("User1");
            var firstDeletedDate = entity.DeletedDate;

            // Act
            entity.SoftDelete("User2");

            // Assert
            entity.IsDeleted.Should().BeTrue();
            entity.DeletedBy.Should().Be("User2");
            entity.DeletedDate.Should().NotBe(firstDeletedDate);
        }
    }
}
