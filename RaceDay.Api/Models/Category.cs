namespace RaceDay.Api.Models;

/// <summary>
/// An age/distance category offered within one Event.
/// Matches the CATEGORIES entity in the Part 1 ERD and the
/// dbo.Categories table in docs/RaceDay_Database.sql.
/// </summary>
public class Category
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    public string CategoryName { get; set; }

    public int MinAge { get; set; } = 0;

    public int MaxAge { get; set; } = 99;

    public decimal DistanceKM { get; set; }

    // Navigation properties
    public Event Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
