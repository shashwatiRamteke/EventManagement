using Microsoft.AspNetCore.Mvc;
using EventManagementApi2.Data;
using EventManagementApi2.Models;
using EventManagementApi2.Services;

namespace EventManagementApi2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventory;

    public TicketsController(IUnitOfWork uow, IInventoryService inventory)
    {
        _uow = uow;
        _inventory = inventory;
    }

    /// <summary>
    /// Purchase one or more tickets for an event
    /// </summary>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(PurchaseTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseTicketResponse>> PurchaseTickets(PurchaseTicketRequest request)
    {
        // Validate event exists
        var evt = await _uow.Events.GetByIdAsync(request.EventId);
        if (evt is null)
            return NotFound(new { error = "Event not found" });

        // Validate category is associated with this event and tier
        var category = await _uow.TierCategories.GetByIdAsync(request.CategoryId);
        if (category is null || category.TierId != evt.TierId)
            return BadRequest(new { error = "Category not found or not associated with this tier" });

        // Check if category is in the event's selected categories
        var eventHasCategory = evt.EventTierCategories
            .Any(etc => etc.TierCategoryId == request.CategoryId);

        if (!eventHasCategory)
            return BadRequest(new { error = "This category is not available for this event" });

        // Per-category capacity is stored on the event-category link (the authoritative cap).
        var eventCategory = evt.EventTierCategories
            .First(etc => etc.TierCategoryId == request.CategoryId);
        var maxTicketsForCategory = eventCategory.MaxTicketsPerCategory;

        // Check category-level limit
        var soldTicketsInCategory = await _uow.Tickets.CountByEventAndCategoryAsync(request.EventId, request.CategoryId);

        if (soldTicketsInCategory + request.Quantity > maxTicketsForCategory)
            return BadRequest(new { error = $"Only {maxTicketsForCategory - soldTicketsInCategory} tickets available for this category. Each category has a limit of {maxTicketsForCategory} tickets." });

        // Check event-level total capacity
        var totalSoldTickets = await _uow.Tickets.CountByEventAsync(request.EventId);

        if (totalSoldTickets + request.Quantity > evt.TotalTicketing)
            return BadRequest(new { error = $"Only {evt.TotalTicketing - totalSoldTickets} tickets remaining for this event (total capacity: {evt.TotalTicketing})" });

        // Seed the per-category slot with the per-category remaining capacity (not the event
        // total), so the atomic reservation enforces the same limit as the checks above.
        await _inventory.EnsureSeededAsync(request.EventId, request.CategoryId, maxTicketsForCategory - soldTicketsInCategory);

        // Atomically reserve inventory. This is what prevents overselling under concurrency:
        // only one request can win the last available tickets.
        var reservation = await _inventory.ReserveAsync(request.EventId, request.CategoryId, request.Quantity);

        if (reservation.Status == ReservationStatus.InsufficientInventory)
            return BadRequest(new { error = $"Only {reservation.Remaining} tickets available for this category" });

        if (reservation.Status != ReservationStatus.Reserved || reservation.HoldId is null)
            return BadRequest(new { error = "Unable to reserve tickets. Please try again." });

        var holdId = reservation.HoldId;

        try
        {
            // Generate ticket price (simplified - in real app, prices may vary by category)
            decimal pricePerTicket = 50m; // Default price

            var tickets = new List<Ticket>();
            var ticketResponses = new List<TicketResponse>();

            for (int i = 0; i < request.Quantity; i++)
            {
                var ticket = new Ticket
                {
                    TicketNumber = GenerateTicketNumber(request.EventId, request.CategoryId),
                    EventId = request.EventId,
                    CategoryId = request.CategoryId,
                    BuyerName = request.BuyerName,
                    BuyerEmail = request.BuyerEmail,
                    BuyerPhone = request.BuyerPhone,
                    Price = pricePerTicket,
                    PurchaseDate = DateTime.UtcNow,
                    Status = TicketStatus.Active,
                    Notes = request.Notes
                };

                tickets.Add(ticket);

                ticketResponses.Add(new TicketResponse
                {
                    Id = ticket.Id,
                    TicketNumber = ticket.TicketNumber,
                    EventId = ticket.EventId,
                    EventName = evt.Name,
                    CategoryId = ticket.CategoryId,
                    CategoryName = category.Name,
                    BuyerEmail = ticket.BuyerEmail,
                    BuyerName = ticket.BuyerName,
                    BuyerPhone = ticket.BuyerPhone,
                    Price = ticket.Price,
                    PurchaseDate = ticket.PurchaseDate,
                    Status = ticket.Status,
                    Notes = ticket.Notes
                });
            }

            await _uow.Tickets.CreateTicketsAsync(tickets);

            // Purchase persisted successfully - confirm the hold so the inventory stays consumed.
            await _inventory.ConfirmAsync(holdId);

            var totalPrice = tickets.Sum(t => t.Price);

            var response = new PurchaseTicketResponse
            {
                Success = true,
                Message = $"Successfully purchased {request.Quantity} ticket(s)",
                Tickets = ticketResponses,
                TotalPrice = totalPrice
            };

            return CreatedAtAction(nameof(GetTicketsByEmail), new { email = request.BuyerEmail }, response);
        }
        catch
        {
            // Persisting failed - release the reserved inventory back to the pool.
            await _inventory.ReleaseAsync(holdId);
            throw;
        }
    }

    /// <summary>
    /// Get all tickets by buyer email
    /// </summary>
    [HttpGet("buyer/{email}")]
    [ProducesResponseType(typeof(TicketListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketListResponse>> GetTicketsByEmail(string email)
    {
        var tickets = await _uow.Tickets.GetByBuyerEmailAsync(email);

        if (tickets.Count == 0)
            return NotFound(new { error = "No tickets found for this email" });

        var responses = tickets.Select(t => new TicketResponse
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            EventId = t.EventId,
            EventName = t.Event.Name,
            CategoryId = t.CategoryId,
            CategoryName = t.Category.Name,
            BuyerEmail = t.BuyerEmail,
            BuyerName = t.BuyerName,
            BuyerPhone = t.BuyerPhone,
            Price = t.Price,
            PurchaseDate = t.PurchaseDate,
            Status = t.Status,
            Notes = t.Notes
        }).ToList();

        return Ok(new TicketListResponse
        {
            Total = responses.Count,
            Tickets = responses
        });
    }

    /// <summary>
    /// Get a specific ticket by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> GetTicket(int id)
    {
        var ticket = await _uow.Tickets.GetByIdAsync(id);

        if (ticket is null)
            return NotFound(new { error = "Ticket not found" });

        var response = new TicketResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            EventId = ticket.EventId,
            EventName = ticket.Event.Name,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category.Name,
            BuyerEmail = ticket.BuyerEmail,
            BuyerName = ticket.BuyerName,
            BuyerPhone = ticket.BuyerPhone,
            Price = ticket.Price,
            PurchaseDate = ticket.PurchaseDate,
            Status = ticket.Status,
            Notes = ticket.Notes
        };

        return Ok(response);
    }

    /// <summary>
    /// Get tickets for a specific event
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    [ProducesResponseType(typeof(TicketListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketListResponse>> GetEventTickets(int eventId)
    {
        var evt = await _uow.Events.GetByIdAsync(eventId);
        if (evt is null)
            return NotFound(new { error = "Event not found" });

        var tickets = await _uow.Tickets.GetByEventIdAsync(eventId);

        var responses = tickets.Select(t => new TicketResponse
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            EventId = t.EventId,
            EventName = t.Event.Name,
            CategoryId = t.CategoryId,
            CategoryName = t.Category.Name,
            BuyerEmail = t.BuyerEmail,
            BuyerName = t.BuyerName,
            BuyerPhone = t.BuyerPhone,
            Price = t.Price,
            PurchaseDate = t.PurchaseDate,
            Status = t.Status,
            Notes = t.Notes
        }).ToList();

        return Ok(new TicketListResponse
        {
            Total = responses.Count,
            Tickets = responses
        });
    }

    /// <summary>
    /// Mark a ticket as used
    /// </summary>
    [HttpPatch("{id:int}/mark-used")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> MarkTicketAsUsed(int id)
    {
        var ticket = await _uow.Tickets.GetByIdAsync(id);

        if (ticket is null)
            return NotFound(new { error = "Ticket not found" });

        if (ticket.Status != TicketStatus.Active)
            return BadRequest(new { error = $"Cannot mark ticket as used. Current status: {ticket.Status}" });

        await _uow.Tickets.UpdateStatusAsync(ticket, TicketStatus.Used);

        var response = new TicketResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            EventId = ticket.EventId,
            EventName = ticket.Event.Name,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category.Name,
            BuyerEmail = ticket.BuyerEmail,
            BuyerName = ticket.BuyerName,
            BuyerPhone = ticket.BuyerPhone,
            Price = ticket.Price,
            PurchaseDate = ticket.PurchaseDate,
            Status = ticket.Status,
            Notes = ticket.Notes
        };

        return Ok(response);
    }

    /// <summary>
    /// Cancel a ticket (for refund)
    /// </summary>
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> CancelTicket(int id)
    {
        var ticket = await _uow.Tickets.GetByIdAsync(id);

        if (ticket is null)
            return NotFound(new { error = "Ticket not found" });

        if (ticket.Status == TicketStatus.Used)
            return BadRequest(new { error = "Cannot cancel a ticket that has already been used" });

        if (ticket.Status == TicketStatus.Cancelled || ticket.Status == TicketStatus.Refunded)
            return BadRequest(new { error = $"Ticket is already {ticket.Status}" });

        await _uow.Tickets.UpdateStatusAsync(ticket, TicketStatus.Cancelled);

        var response = new TicketResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            EventId = ticket.EventId,
            EventName = ticket.Event.Name,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category.Name,
            BuyerEmail = ticket.BuyerEmail,
            BuyerName = ticket.BuyerName,
            BuyerPhone = ticket.BuyerPhone,
            Price = ticket.Price,
            PurchaseDate = ticket.PurchaseDate,
            Status = ticket.Status,
            Notes = ticket.Notes
        };

        return Ok(response);
    }

    /// <summary>
    /// Get ticket statistics for an event
    /// </summary>
    [HttpGet("statistics/event/{eventId:int}")]
    [ProducesResponseType(typeof(TicketStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketStatistics>> GetEventStatistics(int eventId)
    {
        var evt = await _uow.Events.GetByIdAsync(eventId);
        if (evt is null)
            return NotFound(new { error = "Event not found" });

        var tickets = await _uow.Tickets.GetByEventIdRawAsync(eventId);

        var stats = new TicketStatistics
        {
            EventId = eventId,
            EventName = evt.Name,
            TotalTicketsAvailable = evt.TotalTicketing,
            TotalTicketsSold = tickets.Count,
            TotalTicketsActive = tickets.Count(t => t.Status == TicketStatus.Active),
            TotalTicketsUsed = tickets.Count(t => t.Status == TicketStatus.Used),
            TotalTicketsCancelled = tickets.Count(t => t.Status == TicketStatus.Cancelled),
            TotalTicketsRefunded = tickets.Count(t => t.Status == TicketStatus.Refunded),
            TotalRevenue = tickets.Where(t => t.Status != TicketStatus.Cancelled && t.Status != TicketStatus.Refunded).Sum(t => t.Price),
            AvailableTickets = evt.TotalTicketing - tickets.Count
        };

        return Ok(stats);
    }

    private string GenerateTicketNumber(int eventId, int categoryId)
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var random = new Random().Next(1000, 9999);
        return $"TKT-{eventId:D4}-{categoryId:D3}-{timestamp:D10}-{random}";
    }
}

public class TicketStatistics
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int TotalTicketsAvailable { get; set; }
    public int TotalTicketsSold { get; set; }
    public int TotalTicketsActive { get; set; }
    public int TotalTicketsUsed { get; set; }
    public int TotalTicketsCancelled { get; set; }
    public int TotalTicketsRefunded { get; set; }
    public decimal TotalRevenue { get; set; }
    public int AvailableTickets { get; set; }
}