namespace RaceDay.Api.Models;

/// <summary>
/// An Event Organiser: creates and manages Events, Categories and Results.
/// Matches the ORGANISERS entity in the Part 1 ERD and the dbo.Organisers
/// table in docs/RaceDay_Database.sql.
/// </summary>
public class Organiser
{
    public int OrganiserID { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string PhoneNumber { get; set; }

    public string OrganisationName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
