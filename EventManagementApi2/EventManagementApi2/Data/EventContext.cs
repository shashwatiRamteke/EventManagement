using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Models;

namespace EventManagementApi2.Data;

public class EventContext : DbContext
{
    public EventContext(DbContextOptions<EventContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;
}
