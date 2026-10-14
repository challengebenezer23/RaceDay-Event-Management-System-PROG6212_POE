namespace RaceDay.Api.Models;

/// <summary>
/// The finish time and finishing position produced by one Enrolment,
/// captured by the owning Organiser. Matches the RESULTS entity in the
/// Part 1 ERD and the dbo.Results table in docs/RaceDay_Database.sql.
/// </summary>
public class Result
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    public int FinishTimeSeconds { get; set; }

    public int FinishPosition { get; set; }

    public int RecordedByOrganiserID { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Enrolment Enrolment { get; set; }
    public Organiser RecordedByOrganiser { get; set; }
}
