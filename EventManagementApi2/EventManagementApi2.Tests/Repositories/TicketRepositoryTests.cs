using EventManagementApi2.Data.Repositories;
using EventManagementApi2.Models;
using EventManagementApi2.Tests.Helpers;
using FluentAssertions;

namespace EventManagementApi2.Tests.Repositories;

public class TicketRepositoryTests : IDisposable
{
    private readonly EventManagementApi2.Data.EventContext _context;
    private readonly TicketRepository _repository;

    public TicketRepositoryTests()
    {
        _context = TestDataBuilder.CreateInMemoryContext($"TicketRepoTest_{Guid.NewGuid()}");
        _repository = new TicketRepository(_context);
    }

    [Fact]
    public async Task CountByEventAndCategoryAsync_ReturnsCorrectCount()
    {
        // Arrange
        TestDataBuilder.SeedTestData(_context);
        var ticket1 = TestDataBuilder.CreateTicket(1, 1, 1, "buyer1@test.com");
        var ticket2 = TestDataBuilder.CreateTicket(2, 1, 1, "buyer2@test.com");

        _context.Tickets.AddRange(ticket1, ticket2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountByEventAndCategoryAsync(1, 1);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountByEventAndCategoryAsync_WhenNoTickets_ReturnsZero()
    {
        // Act
        var count = await _repository.CountByEventAndCategoryAsync(1, 1);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleTickets()
    {
        // Arrange
        TestDataBuilder.SeedTestData(_context);
        var tickets = new[]
        {
            TestDataBuilder.CreateTicket(1, 1, 1, "buyer1@test.com"),
            TestDataBuilder.CreateTicket(2, 1, 1, "buyer2@test.com")
        };

        // Act
        await TestDataBuilder.AddTicketsAsync(_context, tickets);

        // Assert
        var count = await _repository.CountByEventAndCategoryAsync(1, 1);
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetByBuyerEmailAsync_ReturnsTicketsForEmail()
    {
        // Arrange
        TestDataBuilder.SeedTestData(_context);
        var ticket1 = TestDataBuilder.CreateTicket(1, 1, 1, "buyer@test.com");
        var ticket2 = TestDataBuilder.CreateTicket(2, 1, 1, "buyer@test.com");
        var ticket3 = TestDataBuilder.CreateTicket(3, 1, 1, "other@test.com");

        _context.Tickets.AddRange(ticket1, ticket2, ticket3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByBuyerEmailAsync("buyer@test.com");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.BuyerEmail.Should().Be("buyer@test.com"));
    }

    [Fact]
    public async Task GetByBuyerEmailAsync_WhenNoTickets_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByBuyerEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTicketExists_ReturnsTicketWithNavigationProperties()
    {
        // Arrange
        TestDataBuilder.SeedTestData(_context);
        var ticket = TestDataBuilder.CreateTicket(1, 1, 1, "buyer@test.com");
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Event.Should().NotBeNull();
        result.Category.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTicketDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEventIdAsync_ReturnsAllTicketsForEvent()
    {
        // Arrange
        TestDataBuilder.SeedTestData(_context);
        var ticket1 = TestDataBuilder.CreateTicket(1, 1, 1, "buyer1@test.com");
        var ticket2 = TestDataBuilder.CreateTicket(2, 1, 1, "buyer2@test.com");

        _context.Tickets.AddRange(ticket1, ticket2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEventIdAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.EventId.Should().Be(1));
    }

    [Fact]
    public async Task GetByEventIdRawAsync_ReturnsTicketsWithoutNavigationProperties()
    {
        // Arrange
        TestDataBuilder.SeedTestData(_context);
        var ticket = TestDataBuilder.CreateTicket(1, 1, 1, "buyer@test.com");
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Clear tracking to ensure navigation properties are not auto-loaded
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByEventIdRawAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].EventId.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
