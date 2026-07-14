using EventManagementApi2.Data.Repositories;
using EventManagementApi2.Tests.Helpers;
using FluentAssertions;

namespace EventManagementApi2.Tests.Repositories;

public class TierCategoryRepositoryTests : IDisposable
{
    private readonly EventManagementApi2.Data.EventContext _context;
    private readonly TierCategoryRepository _repository;

    public TierCategoryRepositoryTests()
    {
        _context = TestDataBuilder.CreateInMemoryContext($"TierCategoryRepoTest_{Guid.NewGuid()}");
        _repository = new TierCategoryRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsCategory()
    {
        // Arrange
        var tier = TestDataBuilder.CreateTier(1, "Premium");
        var category = TestDataBuilder.CreateTierCategory(1, 1, "VIP");

        _context.Tiers.Add(tier);
        _context.TierCategories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("VIP");
        result.TierId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
