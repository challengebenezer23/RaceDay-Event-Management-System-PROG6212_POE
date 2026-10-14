using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Profile;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>View and update the profile of whichever role is currently logged in.</summary>
[ApiController]
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns the profile information of the currently authenticated user.</summary>
    /// <response code="200">The caller's profile.</response>
    /// <response code="401">No one is logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var (userId, role) = CurrentUser();

        if (role == RaceDayRoles.Organiser)
        {
            var organiser = await _context.Organisers.FindAsync(userId);
            if (organiser is null)
            {
                return Unauthorized();
            }

            return Ok(ToProfileResponse(organiser));
        }

        var participant = await _context.Participants.FindAsync(userId);
        if (participant is null)
        {
            return Unauthorized();
        }

        return Ok(ToProfileResponse(participant));
    }

    /// <summary>Updates the profile information of the currently authenticated user.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">A field was invalid.</response>
    /// <response code="401">No one is logged in.</response>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var (userId, role) = CurrentUser();

        if (role == RaceDayRoles.Organiser)
        {
            var organiser = await _context.Organisers.FindAsync(userId);
            if (organiser is null)
            {
                return Unauthorized();
            }

            organiser.FullName = request.FullName.Trim();
            organiser.PhoneNumber = request.PhoneNumber;
            organiser.OrganisationName = request.OrganisationName;
            await _context.SaveChangesAsync();

            return Ok(ToProfileResponse(organiser));
        }

        var participant = await _context.Participants.FindAsync(userId);
        if (participant is null)
        {
            return Unauthorized();
        }

        participant.FullName = request.FullName.Trim();
        participant.PhoneNumber = request.PhoneNumber;
        participant.EmergencyContactName = request.EmergencyContactName;
        participant.EmergencyContactPhone = request.EmergencyContactPhone;
        await _context.SaveChangesAsync();

        return Ok(ToProfileResponse(participant));
    }

    private static ProfileResponse ToProfileResponse(Models.Organiser organiser) => new()
    {
        UserId = organiser.OrganiserID,
        Role = RaceDayRoles.Organiser,
        FullName = organiser.FullName,
        Email = organiser.Email,
        PhoneNumber = organiser.PhoneNumber,
        OrganisationName = organiser.OrganisationName,
        CreatedAt = organiser.CreatedAt
    };

    private static ProfileResponse ToProfileResponse(Models.Participant participant) => new()
    {
        UserId = participant.ParticipantID,
        Role = RaceDayRoles.Participant,
        FullName = participant.FullName,
        Email = participant.Email,
        PhoneNumber = participant.PhoneNumber,
        DateOfBirth = participant.DateOfBirth,
        EmergencyContactName = participant.EmergencyContactName,
        EmergencyContactPhone = participant.EmergencyContactPhone,
        CreatedAt = participant.CreatedAt
    };

    private (int UserId, string Role) CurrentUser()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId).Value;
        var role = HttpContext.Session.GetString(SessionKeys.Role);
        return (userId, role);
    }
}
