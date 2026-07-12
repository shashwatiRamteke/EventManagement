using EventManagementApi2.Data.Repositories;

namespace EventManagementApi2.Data;

public interface IUnitOfWork : IDisposable
{
    IEventRepository Events { get; }
    ITierCategoryRepository TierCategories { get; }
    ITicketRepository Tickets { get; }

    Task<int> SaveChangesAsync();
}
