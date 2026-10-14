namespace RaceDay.Api.Dtos.Profile;

/// <summary>
/// Response body for GET /api/profile. Fields that don't apply to the
/// caller's role (e.g. OrganisationName for a Participant) are null.
/// </summary>
public class ProfileResponse
{
    public int UserId { get; set; }
    public string Role { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    // Organiser only
    public string OrganisationName { get; set; }

    // Participant only
    public DateTime? DateOfBirth { get; set; }
    public string EmergencyContactName { get; set; }
    public string EmergencyContactPhone { get; set; }
}
