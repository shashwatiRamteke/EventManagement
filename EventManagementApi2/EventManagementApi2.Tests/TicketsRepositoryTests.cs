using Xunit;
using EventManagementApi2.Data.Repositories;
using EventManagementApi2.Data;
using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Models;

namespace EventManagementApi2.Tests;

public class TicketsRepositoryTests
{
    private EventContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new EventContext(options);
        EventSeeder.Seed(ctx);
        return ctx;
    }

    [Fact]
    public async Task CountByEventAndCategoryAsync_ReturnsCorrectCount()
    {
        using var ctx = CreateContext();
        var repo = new TicketRepository(ctx);

        // Add a ticket for event 1 category 1
        var ticket = new Ticket { EventId = 1, CategoryId = 1, BuyerEmail = "a@b.com", BuyerName = "Test", Price = 10, PurchaseDate = DateTime.UtcNow, Status = TicketStatus.Active };
        await ctx.Tickets.AddAsync(ticket);
        await ctx.SaveChangesAsync();

        var count = await repo.CountByEventAndCategoryAsync(1, 1);
        Assert.Equal(1, count);
    }
}
