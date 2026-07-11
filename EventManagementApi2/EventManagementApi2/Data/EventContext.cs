using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Models;

namespace EventManagementApi2.Data;

public class EventContext : DbContext
{
    public EventContext(DbContextOptions<EventContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Tier> Tiers { get; set; } = null!;
    public DbSet<TierCategory> TierCategories { get; set; } = null!;
    public DbSet<EventTierCategory> EventTierCategories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each event belongs to exactly one tier
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Tier)
            .WithMany(t => t.Events)
            .HasForeignKey(e => e.TierId);

        // Composite PK for the event-category join table
        modelBuilder.Entity<EventTierCategory>()
            .HasKey(etc => new { etc.EventId, etc.TierCategoryId });

        modelBuilder.Entity<EventTierCategory>()
            .HasOne(etc => etc.Event)
            .WithMany(e => e.EventTierCategories)
            .HasForeignKey(etc => etc.EventId);

        modelBuilder.Entity<EventTierCategory>()
            .HasOne(etc => etc.TierCategory)
            .WithMany(tc => tc.EventTierCategories)
            .HasForeignKey(etc => etc.TierCategoryId);
    }
}
