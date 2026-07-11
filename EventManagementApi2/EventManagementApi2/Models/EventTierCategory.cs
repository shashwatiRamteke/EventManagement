namespace EventManagementApi2.Models;

/// <summary>
/// Tracks which TierCategories are selected for a given Event.
/// </summary>
public class EventTierCategory
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int TierCategoryId { get; set; }
    public TierCategory TierCategory { get; set; } = null!;
}
