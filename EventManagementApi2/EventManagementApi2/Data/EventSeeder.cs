using EventManagementApi2.Models;

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

        context.Events.AddRange(
            new Event
            {
                Name = "Tech Summit 2026",
                Description = "Annual technology conference covering AI, cloud, and DevOps trends.",
                Venue = "Convention Center, New York",
                Date = new DateTime(2026, 9, 15),
                Time = new TimeSpan(9, 0, 0),
                TotalTicketing = 500,
                Tier = earlyBirdTier,
                EventTierCategories =
                [
                    new EventTierCategory { TierCategory = earlyBirdCats["EarlyBird"] },
                    new EventTierCategory { TierCategory = earlyBirdCats["Regular"] }
                ]
            },
            new Event
            {
                Name = "Music Fest",
                Description = "Open-air music festival featuring live bands and solo artists.",
                Venue = "Central Park, New York",
                Date = new DateTime(2026, 8, 20),
                Time = new TimeSpan(16, 0, 0),
                TotalTicketing = 2000,
                Tier = accessBasedTier,
                EventTierCategories =
                [
                    new EventTierCategory { TierCategory = accessBasedCats["General"] },
                    new EventTierCategory { TierCategory = accessBasedCats["VIP"] }
                ]
            },
            new Event
            {
                Name = "Startup Pitch Night",
                Description = "Entrepreneurs pitch ideas to investors and industry experts.",
                Venue = "Innovation Hub, San Francisco",
                Date = new DateTime(2026, 10, 5),
                Time = new TimeSpan(18, 30, 0),
                TotalTicketing = 150,
                Tier = accessBasedTier,
                EventTierCategories =
                [
                    new EventTierCategory { TierCategory = accessBasedCats["VIP"] },
                    new EventTierCategory { TierCategory = accessBasedCats["Backstage"] }
                ]
            },
            new Event
            {
                Name = "Health & Wellness Expo",
                Description = "Exhibition on fitness, nutrition, and mental health.",
                Venue = "City Expo Hall, Chicago",
                Date = new DateTime(2026, 11, 12),
                Time = new TimeSpan(10, 0, 0),
                TotalTicketing = 800,
                Tier = groupBulkTier,
                EventTierCategories =
                [
                    new EventTierCategory { TierCategory = groupBulkCats["Group"] },
                    new EventTierCategory { TierCategory = groupBulkCats["Bulk"] }
                ]
            },
            new Event
            {
                Name = "Art & Design Fair",
                Description = "Showcase of contemporary art, photography, and digital design.",
                Venue = "Metropolitan Gallery, Los Angeles",
                Date = new DateTime(2026, 12, 1),
                Time = new TimeSpan(11, 0, 0),
                TotalTicketing = 300,
                Tier = earlyBirdTier,
                EventTierCategories =
                [
                    new EventTierCategory { TierCategory = earlyBirdCats["EarlyBird"] },
                    new EventTierCategory { TierCategory = earlyBirdCats["Regular"] },
                    new EventTierCategory { TierCategory = earlyBirdCats["Late"] }
                ]
            }
        );

        context.SaveChanges();
    }
}
