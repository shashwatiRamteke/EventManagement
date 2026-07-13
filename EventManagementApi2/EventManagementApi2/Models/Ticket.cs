using System.ComponentModel.DataAnnotations;

namespace EventManagementApi2.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required]
    public string TicketNumber { get; set; } = string.Empty;

    [Required]
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    [Required]
    public int CategoryId { get; set; }

    public TierCategory Category { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string BuyerEmail { get; set; } = string.Empty;

    [Required]
    public string BuyerName { get; set; } = string.Empty;

    public string? BuyerPhone { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    public DateTime PurchaseDate { get; set; }

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Active;

    public string? Notes { get; set; }
}

public enum TicketStatus
{
    Active = 1,
    Used = 2,
    Cancelled = 3,
    Refunded = 4
}