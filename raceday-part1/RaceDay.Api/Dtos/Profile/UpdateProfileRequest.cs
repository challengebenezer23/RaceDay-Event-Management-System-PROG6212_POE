using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Profile;

/// <summary>Request body for PUT /api/profile.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; }

    [StringLength(20)]
    public string PhoneNumber { get; set; }

    /// <summary>Organiser only. Ignored for a Participant caller.</summary>
    [StringLength(150)]
    public string OrganisationName { get; set; }

    /// <summary>Participant only. Ignored for an Organiser caller.</summary>
    [StringLength(100)]
    public string EmergencyContactName { get; set; }

    /// <summary>Participant only. Ignored for an Organiser caller.</summary>
    [StringLength(20)]
    public string EmergencyContactPhone { get; set; }
}
