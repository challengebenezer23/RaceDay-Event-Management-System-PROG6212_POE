namespace RaceDay.Api.Dtos.Events;

/// <summary>Response body describing one RaceDay event.</summary>
public class EventResponse
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; }
    public string EventName { get; set; }
    public string Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; }
    public decimal Distance { get; set; }
    public string EventType { get; set; }
    public DateTime CreatedAt { get; set; }
}
