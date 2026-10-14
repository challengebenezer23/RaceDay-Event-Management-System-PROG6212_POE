using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Participants entering events, and Organisers viewing who has enrolled.</summary>
[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Enrols the authenticated Participant into an event under a chosen category.</summary>
    /// <response code="201">The enrolment was created.</response>
    /// <response code="400">A field was missing.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="404">The event or category does not exist (or the category does not belong to the event).</response>
    /// <response code="409">The Participant is already enrolled in this event.</response>
    [HttpPost("enrolments")]
    [SessionAuthorize(RaceDayRoles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnrolment([FromBody] CreateEnrolmentRequest request)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventID == request.EventId);
        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event exists with ID {request.EventId}." });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId && c.EventID == request.EventId);
        if (category is null)
        {
            return NotFound(new { message = "No matching category exists for that event." });
        }

        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(e => e.ParticipantID == participantId && e.EventID == request.EventId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = request.EventId,
            CategoryID = request.CategoryId,
            Status = "Pending"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var participant = await _context.Participants.FindAsync(participantId);

        return StatusCode(StatusCodes.Status201Created, ToResponse(enrolment, participant, raceEvent, category));
    }

    /// <summary>Returns every enrolment belonging to the authenticated Participant.</summary>
    /// <response code="200">The Participant's enrolments.</response>
    /// <response code="401">No one is logged in.</response>
    [HttpGet("enrolments/mine")]
    [SessionAuthorize(RaceDayRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Where(e => e.ParticipantID == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => ToResponse(e, e.Participant, e.Event, e.Category)));
    }

    /// <summary>Returns every Participant enrolment for an event owned by the authenticated Organiser.</summary>
    /// <response code="200">The event's enrolments.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event exists with ID {eventId}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        }

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Where(e => e.EventID == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => ToResponse(e, e.Participant, raceEvent, e.Category)));
    }

    /// <summary>Withdraws the authenticated Participant's own enrolment before the event date.</summary>
    /// <response code="200">The enrolment was withdrawn.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own this enrolment.</response>
    /// <response code="404">No enrolment exists with that ID.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize(RaceDayRoles.Participant)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> WithdrawEnrolment(int id)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);

        if (enrolment is null)
        {
            return NotFound(new { message = $"No enrolment exists with ID {id}." });
        }

        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (enrolment.ParticipantID != participantId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This is not your enrolment." });
        }

        if (enrolment.Event.EventDate.Date <= DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "This event has already taken place; the enrolment can no longer be withdrawn." });
        }

        if (enrolment.Result is not null)
        {
            return Conflict(new { message = "A result has already been recorded for this enrolment." });
        }

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Enrolment withdrawn." });
    }

    private static EnrolmentResponse ToResponse(Enrolment e, Participant participant, Event raceEvent, Category category) => new()
    {
        EnrolmentID = e.EnrolmentID,
        ParticipantID = e.ParticipantID,
        ParticipantName = participant?.FullName,
        EventID = e.EventID,
        EventName = raceEvent?.EventName,
        EventDate = raceEvent?.EventDate ?? default,
        CategoryID = e.CategoryID,
        CategoryName = category?.CategoryName,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };
}
