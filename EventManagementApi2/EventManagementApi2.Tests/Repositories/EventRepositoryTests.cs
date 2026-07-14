using EventManagementApi2.Data.Repositories;
using EventManagementApi2.Tests.Helpers;
using FluentAssertions;

namespace EventManagementApi2.Tests.Repositories;

public class EventRepositoryTests : IDisposable
{
    private readonly EventManagementApi2.Data.EventContext _context;
    private readonly EventRepository _repository;

    public EventRepositoryTests()
    {
        _context = TestDataBuilder.CreateInMemoryContext($"EventRepoTest_{Guid.NewGuid()}");
        _repository = new EventRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var tier = TestDataBuilder.CreateTier(1, "Premium");
        var evt = TestDataBuilder.CreateEvent(1, 1, "Test Event");
        var category = TestDataBuilder.CreateTierCategory(1, 1, "VIP");
        var eventTierCategory = TestDataBuilder.CreateEventTierCategory(1, 1);

        _context.Tiers.Add(tier);
        _context.Events.Add(evt);
        _context.TierCategories.Add(category);
        _context.EventTierCategories.Add(eventTierCategory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Event");
        result.EventTierCategories.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEvents()
    {
        // Arrange
        var tier = TestDataBuilder.CreateTier(1, "Premium");
        var evt1 = TestDataBuilder.CreateEvent(1, 1, "Event 1");
        var evt2 = TestDataBuilder.CreateEvent(2, 1, "Event 2");

        _context.Tiers.Add(tier);
        _context.Events.AddRange(evt1, evt2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoEvents_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
