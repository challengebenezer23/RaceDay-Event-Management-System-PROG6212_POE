using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Auth;

/// <summary>Request body for POST /api/auth/register.</summary>
public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; }

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; }

    [StringLength(20)]
    public string PhoneNumber { get; set; }

    /// <summary>Must be exactly "Organiser" or "Participant".</summary>
    [Required]
    public string Role { get; set; }

    /// <summary>Organiser only. Ignored when registering as a Participant.</summary>
    [StringLength(150)]
    public string OrganisationName { get; set; }

    /// <summary>Participant only. Required when registering as a Participant.</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Participant only.</summary>
    [StringLength(100)]
    public string EmergencyContactName { get; set; }

    /// <summary>Participant only.</summary>
    [StringLength(20)]
    public string EmergencyContactPhone { get; set; }
}
