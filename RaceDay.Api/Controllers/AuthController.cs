using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout for both Organisers and Participants.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordHasher<PasswordHasherUser> _passwordHasher;

    public AuthController(RaceDayDbContext context, IPasswordHasher<PasswordHasherUser> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Registers a new user as either an Organiser or a Participant.
    /// </summary>
    /// <response code="201">The account was created.</response>
    /// <response code="400">A required field was missing or invalid.</response>
    /// <response code="409">That email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var normalisedRole = request.Role?.Trim();
        var isOrganiser = string.Equals(normalisedRole, RaceDayRoles.Organiser, StringComparison.OrdinalIgnoreCase);
        var isParticipant = string.Equals(normalisedRole, RaceDayRoles.Participant, StringComparison.OrdinalIgnoreCase);

        if (!isOrganiser && !isParticipant)
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        if (isParticipant && request.DateOfBirth is null)
        {
            return BadRequest(new { message = "DateOfBirth is required when registering as a Participant." });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _context.Organisers.AnyAsync(o => o.Email.ToLower() == email)
            || await _context.Participants.AnyAsync(p => p.Email.ToLower() == email);

        if (emailTaken)
        {
            return Conflict(new { message = "An account with that email address already exists." });
        }

        var passwordHash = _passwordHasher.HashPassword(new PasswordHasherUser(), request.Password);

        if (isOrganiser)
        {
            var organiser = new Organiser
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                OrganisationName = request.OrganisationName
            };

            _context.Organisers.Add(organiser);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                UserId = organiser.OrganiserID,
                Role = RaceDayRoles.Organiser,
                FullName = organiser.FullName,
                Email = organiser.Email,
                Message = "Organiser account created successfully."
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        else
        {
            var participant = new Participant
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth!.Value,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                UserId = participant.ParticipantID,
                Role = RaceDayRoles.Participant,
                FullName = participant.FullName,
                Email = participant.Email,
                Message = "Participant account created successfully."
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
    }

    /// <summary>
    /// Authenticates a registered user and starts a server-side session, returning their role.
    /// </summary>
    /// <response code="200">Login succeeded; a session cookie is set.</response>
    /// <response code="400">Email or password was missing.</response>
    /// <response code="401">The email/password combination is invalid.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email.ToLower() == email);
        if (organiser is not null && VerifyPassword(organiser.PasswordHash, request.Password))
        {
            SignIn(organiser.OrganiserID, RaceDayRoles.Organiser, organiser.FullName, organiser.Email);
            return Ok(new AuthResponse
            {
                UserId = organiser.OrganiserID,
                Role = RaceDayRoles.Organiser,
                FullName = organiser.FullName,
                Email = organiser.Email,
                Message = "Login successful."
            });
        }

        var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email.ToLower() == email);
        if (participant is not null && VerifyPassword(participant.PasswordHash, request.Password))
        {
            SignIn(participant.ParticipantID, RaceDayRoles.Participant, participant.FullName, participant.Email);
            return Ok(new AuthResponse
            {
                UserId = participant.ParticipantID,
                Role = RaceDayRoles.Participant,
                FullName = participant.FullName,
                Email = participant.Email,
                Message = "Login successful."
            });
        }

        return Unauthorized(new { message = "Invalid email or password." });
    }

    /// <summary>Ends the current authenticated user's session.</summary>
    /// <response code="200">The session was cleared.</response>
    /// <response code="401">No one was logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out successfully." });
    }

    private bool VerifyPassword(string storedHash, string suppliedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(new PasswordHasherUser(), storedHash, suppliedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    private void SignIn(int userId, string role, string fullName, string email)
    {
        HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
        HttpContext.Session.SetString(SessionKeys.Role, role);
        HttpContext.Session.SetString(SessionKeys.FullName, fullName ?? string.Empty);
        HttpContext.Session.SetString(SessionKeys.Email, email ?? string.Empty);
    }
}
