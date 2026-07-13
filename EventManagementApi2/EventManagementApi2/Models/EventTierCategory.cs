namespace EventManagementApi2.Models;

/// <summary>
/// Tracks which TierCategories are selected for a given Event.
/// Each category gets an equal share of the event's total capacity.
/// </summary>
public class EventTierCategory
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int TierCategoryId { get; set; }
    public TierCategory TierCategory { get; set; } = null!;

    /// <summary>
    /// Maximum tickets available for this category.
    /// Calculated as: Event.TotalTicketing / NumberOfCategories
    /// </summary>
    public int MaxTicketsPerCategory { get; set; }
}