using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Results;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Organisers capture race results; Participants view their own.</summary>
[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Captures the finish time and finishing position for a Participant's
    /// enrolment, recorded by the owning Organiser.
    /// </summary>
    /// <response code="201">The result was recorded.</response>
    /// <response code="400">A field was missing or invalid.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own the event this enrolment belongs to.</response>
    /// <response code="404">No enrolment exists with that ID.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("results")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateResult([FromBody] CreateResultRequest request)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentId);

        if (enrolment is null)
        {
            return NotFound(new { message = $"No enrolment exists with ID {request.EnrolmentId}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (enrolment.Event.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own the event this enrolment belongs to." });
        }

        if (enrolment.Result is not null)
        {
            return Conflict(new { message = "A result has already been captured for this enrolment." });
        }

        var result = new Result
        {
            EnrolmentID = enrolment.EnrolmentID,
            FinishTimeSeconds = request.FinishTimeSeconds,
            FinishPosition = request.FinishPosition,
            RecordedByOrganiserID = organiserId
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(result, enrolment));
    }

    /// <summary>Corrects a previously captured result for an event owned by the authenticated Organiser.</summary>
    /// <response code="200">The updated result.</response>
    /// <response code="400">A field was invalid.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own the event this result belongs to.</response>
    /// <response code="404">No result exists with that ID.</response>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateResultRequest request)
    {
        var result = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .FirstOrDefaultAsync(r => r.ResultID == id);

        if (result is null)
        {
            return NotFound(new { message = $"No result exists with ID {id}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (result.Enrolment.Event.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own the event this result belongs to." });
        }

        result.FinishTimeSeconds = request.FinishTimeSeconds;
        result.FinishPosition = request.FinishPosition;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(result, result.Enrolment));
    }

    /// <summary>Returns the authenticated Participant's own race results and performance history.</summary>
    /// <response code="200">The Participant's results.</response>
    /// <response code="401">No one is logged in.</response>
    [HttpGet("results/mine")]
    [SessionAuthorize(RaceDayRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .Where(r => r.Enrolment.ParticipantID == participantId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync();

        return Ok(results.Select(r => ToResponse(r, r.Enrolment)));
    }

    /// <summary>Returns every captured result for an event owned by the authenticated Organiser.</summary>
    /// <response code="200">The event's results.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("events/{eventId:int}/results")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResultsForEvent(int eventId)
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

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .Where(r => r.Enrolment.EventID == eventId)
            .OrderBy(r => r.FinishPosition)
            .ToListAsync();

        return Ok(results.Select(r => ToResponse(r, r.Enrolment)));
    }

    private static ResultResponse ToResponse(Result r, Enrolment enrolment) => new()
    {
        ResultID = r.ResultID,
        EnrolmentID = r.EnrolmentID,
        ParticipantID = enrolment?.ParticipantID ?? 0,
        ParticipantName = enrolment?.Participant?.FullName,
        EventID = enrolment?.EventID ?? 0,
        EventName = enrolment?.Event?.EventName,
        CategoryName = enrolment?.Category?.CategoryName,
        FinishTimeSeconds = r.FinishTimeSeconds,
        FinishPosition = r.FinishPosition,
        RecordedByOrganiserID = r.RecordedByOrganiserID,
        RecordedAt = r.RecordedAt
    };
}
