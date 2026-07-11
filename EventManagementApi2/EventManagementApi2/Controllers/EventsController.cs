using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Data;
using EventManagementApi2.Models;

namespace EventManagementApi2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventContext _context;

    public EventsController(EventContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.Tier)
            .Include(e => e.EventTierCategories)
                .ThenInclude(etc => etc.TierCategory)
            .ToListAsync();

        return Ok(events.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponse>> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Tier)
            .Include(e => e.EventTierCategories)
                .ThenInclude(etc => etc.TierCategory)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound();

        return Ok(ToResponse(ev));
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest request)
    {
        var tier = await _context.Tiers.FindAsync(request.TierId);
        if (tier is null)
            return BadRequest($"Tier with id {request.TierId} not found.");

        var categories = await _context.TierCategories
            .Where(tc => request.TierCategoryIds.Contains(tc.Id) && tc.TierId == request.TierId)
            .ToListAsync();

        if (categories.Count != request.TierCategoryIds.Count)
            return BadRequest("One or more TierCategoryIds are invalid or do not belong to the specified tier.");

        var ev = new Event
        {
            Name = request.Name,
            Description = request.Description,
            Venue = request.Venue,
            Date = request.Date,
            Time = request.Time,
            TotalTicketing = request.TotalTicketing,
            Tier = tier,
            EventTierCategories = categories
                .Select(c => new EventTierCategory { TierCategory = c })
                .ToList()
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, ToResponse(ev));
    }

    private static EventResponse ToResponse(Event ev)
    {
        TierResponse? tier = null;

        if (ev.Tier is not null)
        {
            var selectedCategories = ev.EventTierCategories
                .Select(etc => new TierCategoryResponse(etc.TierCategory.Id, etc.TierCategory.Name))
                .ToList();

            tier = new TierResponse(
                ev.Tier.Id,
                ev.Tier.Name,
                ev.Tier.TierType.ToString(),
                selectedCategories);
        }

        return new EventResponse(
            ev.Id,
            ev.Name,
            ev.Description,
            ev.Venue,
            ev.Date,
            ev.Time,
            ev.TotalTicketing,
            tier);
    }
}
