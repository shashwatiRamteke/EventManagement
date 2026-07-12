using EventManagementApi2.Data.Repositories;

namespace EventManagementApi2.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly EventContext _context;

    public IEventRepository Events { get; }
    public ITierCategoryRepository TierCategories { get; }
    public ITicketRepository Tickets { get; }

    public UnitOfWork(EventContext context, IEventRepository events, ITierCategoryRepository tierCategories, ITicketRepository tickets)
    {
        _context = context;
        Events = events;
        TierCategories = tierCategories;
        Tickets = tickets;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
