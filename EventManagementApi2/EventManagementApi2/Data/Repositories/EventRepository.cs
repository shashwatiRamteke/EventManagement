using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Models;

namespace EventManagementApi2.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly EventContext _context;

    public EventRepository(EventContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.EventTierCategories)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _context.Events.ToListAsync();
    }

    public async Task<List<Event>> GetAllWithDetailsAsync()
    {
        return await _context.Events
            .Include(e => e.Tier)
            .Include(e => e.EventTierCategories)
                .ThenInclude(etc => etc.TierCategory)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Tier)
            .Include(e => e.EventTierCategories)
                .ThenInclude(etc => etc.TierCategory)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Tier?> GetTierByIdAsync(int tierId)
    {
        return await _context.Tiers.FindAsync(tierId);
    }

    public async Task<List<TierCategory>> GetTierCategoriesAsync(IReadOnlyList<int> categoryIds, int tierId)
    {
        return await _context.TierCategories
            .Where(tc => categoryIds.Contains(tc.Id) && tc.TierId == tierId)
            .ToListAsync();
    }

    public async Task AddAsync(Event ev)
    {
        _context.Events.Add(ev);
        await _context.SaveChangesAsync();
    }
}