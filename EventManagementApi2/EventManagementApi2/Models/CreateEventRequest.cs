using System.ComponentModel.DataAnnotations;

namespace EventManagementApi2.Models;

public record CreateEventRequest(
    [Required] string Name,
    string? Description,
    string? Venue,
    DateTime Date,
    TimeSpan Time,
    int TotalTicketing,
    int TierId,
    IReadOnlyList<int> TierCategoryIds);
