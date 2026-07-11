namespace EventManagementApi2.Models;

public record TierCategoryResponse(int Id, string Name);

public record TierResponse(int Id, string Name, string TierType, IReadOnlyList<TierCategoryResponse> SelectedCategories);

public record EventResponse(
    int Id,
    string Name,
    string? Description,
    string? Venue,
    DateTime Date,
    TimeSpan Time,
    int TotalTicketing,
    TierResponse? Tier);
