using EventManagementApi2.Models;
namespace EventManagementApi2.Data.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id);
    Task<IEnumerable<Event>> GetAllAsync();
    Task<List<Event>> GetAllWithDetailsAsync();
    Task<Event?> GetByIdWithDetailsAsync(int id);
    Task<Tier?> GetTierByIdAsync(int tierId);
    Task<List<TierCategory>> GetTierCategoriesAsync(IReadOnlyList<int> categoryIds, int tierId);
    Task AddAsync(Event ev);
}