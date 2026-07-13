using System.ComponentModel.DataAnnotations;

namespace EventManagementApi2.Models;

public class PurchaseTicketRequest
{
    [Required(ErrorMessage = "Event ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Event ID must be valid")]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Category ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be valid")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Buyer name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string BuyerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Buyer email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string BuyerEmail { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    public string? BuyerPhone { get; set; }

    public string? Notes { get; set; }
}

public class TicketResponse
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public decimal Price { get; set; }
    public DateTime PurchaseDate { get; set; }
    public TicketStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseTicketResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TicketResponse> Tickets { get; set; } = [];
    public decimal TotalPrice { get; set; }
}

public class TicketListResponse
{
    public int Total { get; set; }
    public List<TicketResponse> Tickets { get; set; } = [];
}