using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Models;

namespace EventManagementApi2.Data.Repositories;

public class TierCategoryRepository : ITierCategoryRepository
{
    private readonly EventContext _context;

    public TierCategoryRepository(EventContext context)
    {
        _context = context;
    }

    public async Task<TierCategory?> GetByIdAsync(int id)
    {
        return await _context.TierCategories.FirstOrDefaultAsync(tc => tc.Id == id);
    }
}
