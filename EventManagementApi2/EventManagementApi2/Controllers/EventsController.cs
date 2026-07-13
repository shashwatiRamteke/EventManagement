using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Data;
using EventManagementApi2.Models;
using EventManagementApi2.Services;

namespace EventManagementApi2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventory;

    public EventsController(IUnitOfWork uow, IInventoryService inventory)
    {
        _uow = uow;
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents()
    {
        var events = await _uow.Events.GetAllWithDetailsAsync();

        return Ok(events.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponse>> GetEvent(int id)
    {
        var ev = await _uow.Events.GetByIdWithDetailsAsync(id);

        if (ev == null)
            return NotFound();

        return Ok(ToResponse(ev));
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest request)
    {
        var tier = await _uow.Events.GetTierByIdAsync(request.TierId);
        if (tier is null)
            return BadRequest($"Tier with id {request.TierId} not found.");

        var categories = await _uow.Events.GetTierCategoriesAsync(request.TierCategoryIds, request.TierId);

        if (categories.Count != request.TierCategoryIds.Count)
            return BadRequest("One or more TierCategoryIds are invalid or do not belong to the specified tier.");

        // Calculate equal distribution of tickets among categories
        var ticketsPerCategory = request.TotalTicketing / categories.Count;
        var remainder = request.TotalTicketing % categories.Count;

        var eventTierCategories = new List<EventTierCategory>();
        for (int i = 0; i < categories.Count; i++)
        {
            // First 'remainder' categories get one extra ticket
            var maxTickets = ticketsPerCategory + (i < remainder ? 1 : 0);
            eventTierCategories.Add(new EventTierCategory
            {
                TierCategory = categories[i],
                MaxTicketsPerCategory = maxTickets
            });
        }

        var ev = new Event
        {
            Name = request.Name,
            Description = request.Description,
            Venue = request.Venue,
            Date = request.Date,
            Time = request.Time,
            TotalTicketing = request.TotalTicketing,
            Tier = tier,
            EventTierCategories = eventTierCategories
        };

        await _uow.Events.AddAsync(ev);

        // Seed the inventory counter for each category with its per-category capacity so
        // purchases can reserve against the correct limit immediately.
        foreach (var etc in eventTierCategories)
        {
            await _inventory.EnsureSeededAsync(ev.Id, etc.TierCategory.Id, etc.MaxTicketsPerCategory);
        }

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