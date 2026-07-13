using EventManagementApi2.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementApi2.Data;

public static class EventSeeder
{
    public static void Seed(EventContext context)
    {
        SeedTiers(context);
        SeedEvents(context);
    }

    private static void SeedTiers(EventContext context)
    {
        if (context.Tiers.Any())
            return;

        context.Tiers.AddRange(
            new Tier
            {
                Name = "EarlyBirdTier",
                TierType = TierType.EarlyBirdTier,
                Categories =
                [
                    new TierCategory { Name = "EarlyBird" },
                    new TierCategory { Name = "Regular" },
                    new TierCategory { Name = "Late" }
                ]
            },
            new Tier
            {
                Name = "AccessBasedTier",
                TierType = TierType.AccessBasedTier,
                Categories =
                [
                    new TierCategory { Name = "General" },
                    new TierCategory { Name = "VIP" },
                    new TierCategory { Name = "Backstage" }
                ]
            },
            new Tier
            {
                Name = "GroupAndBulkTier",
                TierType = TierType.GroupAndBulkTier,
                Categories =
                [
                    new TierCategory { Name = "Individual" },
                    new TierCategory { Name = "Group" },
                    new TierCategory { Name = "Bulk" }
                ]
            }
        );

        context.SaveChanges();
    }

    private static void SeedEvents(EventContext context)
    {
        if (context.Events.Any())
            return;

        var earlyBirdTier = context.Tiers
            .First(t => t.TierType == TierType.EarlyBirdTier);
        var accessBasedTier = context.Tiers
            .First(t => t.TierType == TierType.AccessBasedTier);
        var groupBulkTier = context.Tiers
            .First(t => t.TierType == TierType.GroupAndBulkTier);

        // Load categories per tier so we can reference them by name
        var earlyBirdCats = context.TierCategories
            .Where(tc => tc.TierId == earlyBirdTier.Id)
            .ToDictionary(tc => tc.Name);
        var accessBasedCats = context.TierCategories
            .Where(tc => tc.TierId == accessBasedTier.Id)
            .ToDictionary(tc => tc.Name);
        var groupBulkCats = context.TierCategories
            .Where(tc => tc.TierId == groupBulkTier.Id)
            .ToDictionary(tc => tc.Name);

        // NOTE: TotalTicketing is equally distributed among selected categories by the Redis seeding logic.
        // Use values evenly divisible by the number of categories for cleaner distribution.
        // Example: 20 tickets / 2 categories = 10 tickets per category
        //          300 tickets / 3 categories = 100 tickets per category

        var event1Categories = new List<EventTierCategory>
        {
            new EventTierCategory { TierCategory = earlyBirdCats["EarlyBird"], MaxTicketsPerCategory = 10 },
            new EventTierCategory { TierCategory = earlyBirdCats["Regular"], MaxTicketsPerCategory = 10 }
        };

        var event2Categories = new List<EventTierCategory>
        {
            new EventTierCategory { TierCategory = accessBasedCats["General"], MaxTicketsPerCategory = 1000 },
            new EventTierCategory { TierCategory = accessBasedCats["VIP"], MaxTicketsPerCategory = 1000 }
        };

        var event3Categories = new List<EventTierCategory>
        {
            new EventTierCategory { TierCategory = accessBasedCats["VIP"], MaxTicketsPerCategory = 75 },
            new EventTierCategory { TierCategory = accessBasedCats["Backstage"], MaxTicketsPerCategory = 75 }
        };

        var event4Categories = new List<EventTierCategory>
        {
            new EventTierCategory { TierCategory = groupBulkCats["Group"], MaxTicketsPerCategory = 400 },
            new EventTierCategory { TierCategory = groupBulkCats["Bulk"], MaxTicketsPerCategory = 400 }
        };

        var event5Categories = new List<EventTierCategory>
        {
            new EventTierCategory { TierCategory = earlyBirdCats["EarlyBird"], MaxTicketsPerCategory = 100 },
            new EventTierCategory { TierCategory = earlyBirdCats["Regular"], MaxTicketsPerCategory = 100 },
            new EventTierCategory { TierCategory = earlyBirdCats["Late"], MaxTicketsPerCategory = 100 }
        };

        context.Events.AddRange(
            new Event
            {
                Name = "Tech Summit 2026",
                Description = "Annual technology conference covering AI, cloud, and DevOps trends.",
                Venue = "Convention Center, New York",
                Date = new DateTime(2026, 9, 15),
                Time = new TimeSpan(9, 0, 0),
                TotalTicketing = 20,  // 2 categories ? 10 tickets each
                Tier = earlyBirdTier,
                EventTierCategories = event1Categories
            },
            new Event
            {
                Name = "Music Fest",
                Description = "Open-air music festival featuring live bands and solo artists.",
                Venue = "Central Park, New York",
                Date = new DateTime(2026, 8, 20),
                Time = new TimeSpan(16, 0, 0),
                TotalTicketing = 2000,  // 2 categories ? 1000 tickets each
                Tier = accessBasedTier,
                EventTierCategories = event2Categories
            },
            new Event
            {
                Name = "Startup Pitch Night",
                Description = "Entrepreneurs pitch ideas to investors and industry experts.",
                Venue = "Innovation Hub, San Francisco",
                Date = new DateTime(2026, 10, 5),
                Time = new TimeSpan(18, 30, 0),
                TotalTicketing = 150,  // 2 categories ? 75 tickets each
                Tier = accessBasedTier,
                EventTierCategories = event3Categories
            },
            new Event
            {
                Name = "Health & Wellness Expo",
                Description = "Exhibition on fitness, nutrition, and mental health.",
                Venue = "City Expo Hall, Chicago",
                Date = new DateTime(2026, 11, 12),
                Time = new TimeSpan(10, 0, 0),
                TotalTicketing = 800,  // 2 categories ? 400 tickets each
                Tier = groupBulkTier,
                EventTierCategories = event4Categories
            },
            new Event
            {
                Name = "Art & Design Fair",
                Description = "Showcase of contemporary art, photography, and digital design.",
                Venue = "Metropolitan Gallery, Los Angeles",
                Date = new DateTime(2026, 12, 1),
                Time = new TimeSpan(11, 0, 0),
                TotalTicketing = 300,  // 3 categories ? 100 tickets each
                Tier = earlyBirdTier,
                EventTierCategories = event5Categories
            }
        );

        context.SaveChanges();
    }
}