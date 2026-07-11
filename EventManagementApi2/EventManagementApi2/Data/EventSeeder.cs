using EventManagementApi2.Models;

namespace EventManagementApi2.Data;

public static class EventSeeder
{
    public static void Seed(EventContext context)
    {
        if (context.Events.Any())
            return;

        context.Events.AddRange(
            new Event
            {
                Name = "Tech Summit 2026",
                Description = "Annual technology conference covering AI, cloud, and DevOps trends.",
                Venue = "Convention Center, New York",
                Date = new DateTime(2026, 9, 15),
                Time = new TimeSpan(9, 0, 0),
                TotalTicketing = 500
            },
            new Event
            {
                Name = "Music Fest",
                Description = "Open-air music festival featuring live bands and solo artists.",
                Venue = "Central Park, New York",
                Date = new DateTime(2026, 8, 20),
                Time = new TimeSpan(16, 0, 0),
                TotalTicketing = 2000
            },
            new Event
            {
                Name = "Startup Pitch Night",
                Description = "Entrepreneurs pitch ideas to investors and industry experts.",
                Venue = "Innovation Hub, San Francisco",
                Date = new DateTime(2026, 10, 5),
                Time = new TimeSpan(18, 30, 0),
                TotalTicketing = 150
            },
            new Event
            {
                Name = "Health & Wellness Expo",
                Description = "Exhibition on fitness, nutrition, and mental health.",
                Venue = "City Expo Hall, Chicago",
                Date = new DateTime(2026, 11, 12),
                Time = new TimeSpan(10, 0, 0),
                TotalTicketing = 800
            },
            new Event
            {
                Name = "Art & Design Fair",
                Description = "Showcase of contemporary art, photography, and digital design.",
                Venue = "Metropolitan Gallery, Los Angeles",
                Date = new DateTime(2026, 12, 1),
                Time = new TimeSpan(11, 0, 0),
                TotalTicketing = 300
            }
        );

        context.SaveChanges();
    }
}
