using EventManagementApi2.Models;

namespace EventManagementApi2.Data.Repositories;

public interface ITierCategoryRepository
{
    Task<TierCategory?> GetByIdAsync(int id);
}