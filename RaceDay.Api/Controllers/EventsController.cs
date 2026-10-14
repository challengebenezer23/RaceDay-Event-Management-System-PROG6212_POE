using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Browsing RaceDay events, and creating/updating/deleting them as their owning Organiser.</summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns a list of all RaceDay events.</summary>
    /// <response code="200">The list of events.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.Organiser)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToResponse));
    }

    /// <summary>Returns the details of a specific event using the supplied event ID.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var raceEvent = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event exists with ID {id}." });
        }

        return Ok(ToResponse(raceEvent));
    }

    /// <summary>Creates a new RaceDay event owned by the authenticated Organiser.</summary>
    /// <response code="201">The event was created.</response>
    /// <response code="400">A field was missing or EventType was not Run/Walk/Cycle.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller is not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (!Event.AllowedEventTypes.Contains(request.EventType))
        {
            return BadRequest(new { message = $"EventType must be one of: {string.Join(", ", Event.AllowedEventTypes)}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var raceEvent = new Event
        {
            OrganiserID = organiserId,
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = request.EventType
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        await _context.Entry(raceEvent).Reference(e => e.Organiser).LoadAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(raceEvent));
    }

    /// <summary>Updates an event owned by the authenticated Organiser.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">A field was missing or EventType was not Run/Walk/Cycle.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        if (!Event.AllowedEventTypes.Contains(request.EventType))
        {
            return BadRequest(new { message = $"EventType must be one of: {string.Join(", ", Event.AllowedEventTypes)}." });
        }

        var raceEvent = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event exists with ID {id}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        }

        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = request.Description;
        raceEvent.EventDate = request.EventDate;
        raceEvent.Location = request.Location.Trim();
        raceEvent.Distance = request.Distance;
        raceEvent.EventType = request.EventType;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(raceEvent));
    }

    /// <summary>Deletes an event owned by the authenticated Organiser.</summary>
    /// <response code="200">The event was deleted.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event exists with ID {id}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        }

        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Event deleted." });
    }

    private static EventResponse ToResponse(Event e) => new()
    {
        EventID = e.EventID,
        OrganiserID = e.OrganiserID,
        OrganiserName = e.Organiser?.FullName,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        CreatedAt = e.CreatedAt
    };
}
