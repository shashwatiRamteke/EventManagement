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
    public async Task<ActionResult<EventResponse>> CreateEvent(Event ev)
    {
        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // Reload with tier data so the response includes tier details
        await _context.Entry(ev).Reference(e => e.Tier).LoadAsync();
        await _context.Entry(ev).Collection(e => e.EventTierCategories).Query()
            .Include(etc => etc.TierCategory)
            .LoadAsync();

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
