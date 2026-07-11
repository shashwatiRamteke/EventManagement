using System.ComponentModel.DataAnnotations;

namespace EventManagementApi2.Models;

public class Tier
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public TierType TierType { get; set; }

    public ICollection<TierCategory> Categories { get; set; } = [];

    // Multiple events can share the same tier type
    public ICollection<Event> Events { get; set; } = [];
}
