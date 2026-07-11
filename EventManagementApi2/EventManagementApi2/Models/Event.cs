using System.ComponentModel.DataAnnotations;

namespace EventManagementApi2.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Venue { get; set; }

    // Date-only; the time component is ignored
    public DateTime Date { get; set; }

    // Time of day
    public TimeSpan Time { get; set; }

    public int TotalTicketing { get; set; }

    // Each event has exactly one tier type
    public int TierId { get; set; }
    public Tier Tier { get; set; } = null!;

    // Selected categories from that tier
    public ICollection<EventTierCategory> EventTierCategories { get; set; } = [];
}
