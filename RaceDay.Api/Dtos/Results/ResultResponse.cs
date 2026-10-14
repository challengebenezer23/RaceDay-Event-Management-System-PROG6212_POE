namespace RaceDay.Api.Dtos.Results;

/// <summary>Response body describing one Result.</summary>
public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; }
    public string CategoryName { get; set; }
    public int FinishTimeSeconds { get; set; }
    public int FinishPosition { get; set; }
    public int RecordedByOrganiserID { get; set; }
    public DateTime RecordedAt { get; set; }
}
