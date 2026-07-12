using EventManagementApi2.Models;

namespace EventManagementApi2.Data.Repositories;

public interface ITicketRepository
{
    Task<int> CountByEventAndCategoryAsync(int eventId, int categoryId);
    Task AddRangeAsync(IEnumerable<Ticket> tickets);
    Task<List<Ticket>> GetByBuyerEmailAsync(string email);
    Task<Ticket?> GetByIdAsync(int id);
    Task<List<Ticket>> GetByEventIdAsync(int eventId);
    Task<List<Ticket>> GetByEventIdRawAsync(int eventId);
}
