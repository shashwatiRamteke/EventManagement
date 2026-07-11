using System.ComponentModel.DataAnnotations;

namespace EventManagementApi2.Models;

public class TierCategory
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int TierId { get; set; }

    public Tier Tier { get; set; } = null!;

    public ICollection<EventTierCategory> EventTierCategories { get; set; } = [];

    public ICollection<Ticket> Tickets { get; set; } = [];
}
