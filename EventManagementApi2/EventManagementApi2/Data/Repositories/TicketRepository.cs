using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Models;

namespace EventManagementApi2.Data.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly EventContext _context;

    public TicketRepository(EventContext context)
    {
        _context = context;
    }

    public async Task<int> CountByEventAndCategoryAsync(int eventId, int categoryId)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId && t.CategoryId == categoryId)
            .CountAsync();
    }

    public async Task<int> CountByEventAsync(int eventId)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId)
            .CountAsync();
    }

    public async Task CreateTicketsAsync(IEnumerable<Ticket> tickets)
    {
        await _context.Tickets.AddRangeAsync(tickets);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Ticket ticket, TicketStatus status)
    {
        ticket.Status = status;
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Ticket>> GetByBuyerEmailAsync(string email)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.Category)
            .Where(t => t.BuyerEmail == email)
            .OrderByDescending(t => t.PurchaseDate)
            .ToListAsync();
    }

    public async Task<Ticket?> GetByIdAsync(int id)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Ticket>> GetByEventIdAsync(int eventId)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.Category)
            .Where(t => t.EventId == eventId)
            .OrderByDescending(t => t.PurchaseDate)
            .ToListAsync();
    }

    // Helper for statistics to avoid double-including navigation properties
    public async Task<List<Ticket>> GetByEventIdRawAsync(int eventId)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId)
            .ToListAsync();
    }
}