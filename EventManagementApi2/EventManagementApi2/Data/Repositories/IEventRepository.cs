using EventManagementApi2.Models;
namespace EventManagementApi2.Data.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id);
    Task<IEnumerable<Event>> GetAllAsync();
}
