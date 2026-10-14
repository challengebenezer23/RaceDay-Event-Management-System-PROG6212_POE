namespace RaceDay.Api.Models;

/// <summary>
/// A RaceDay event (a run, walk or cycle), owned by exactly one Organiser.
/// Matches the EVENTS entity in the Part 1 ERD and the dbo.Events table
/// in docs/RaceDay_Database.sql.
/// </summary>
public class Event
{
    public int EventID { get; set; }

    public int OrganiserID { get; set; }

    public string EventName { get; set; }

    public string Description { get; set; }

    public DateTime EventDate { get; set; }

    public string Location { get; set; }

    public decimal Distance { get; set; }

    /// <summary>One of "Run", "Walk" or "Cycle".</summary>
    public string EventType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organiser Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

    public static readonly string[] AllowedEventTypes = { "Run", "Walk", "Cycle" };
}
