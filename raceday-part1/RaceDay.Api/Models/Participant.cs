namespace RaceDay.Api.Models;

/// <summary>
/// A Participant: browses Events, enrols in them, and tracks personal results.
/// Matches the PARTICIPANTS entity in the Part 1 ERD and the
/// dbo.Participants table in docs/RaceDay_Database.sql.
/// </summary>
public class Participant
{
    public int ParticipantID { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string PhoneNumber { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string EmergencyContactName { get; set; }

    public string EmergencyContactPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
