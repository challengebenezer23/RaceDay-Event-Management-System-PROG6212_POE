namespace RaceDay.Api.Models;

/// <summary>
/// The associative entity resolving the many-to-many relationship between
/// Participants and Events, while also recording the selected Category.
/// Matches the ENROLMENTS entity in the Part 1 ERD and the
/// dbo.Enrolments table in docs/RaceDay_Database.sql.
/// </summary>
public class Enrolment
{
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public int EventID { get; set; }

    public int CategoryID { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    /// <summary>One of "Pending", "Confirmed" or "Cancelled".</summary>
    public string Status { get; set; } = "Pending";

    // Navigation properties
    public Participant Participant { get; set; }
    public Event Event { get; set; }
    public Category Category { get; set; }

    /// <summary>The at-most-one Result produced by this Enrolment (1 : 0..1).</summary>
    public Result Result { get; set; }

    public static readonly string[] AllowedStatuses = { "Pending", "Confirmed", "Cancelled" };
}
