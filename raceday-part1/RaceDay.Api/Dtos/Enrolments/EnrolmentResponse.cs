namespace RaceDay.Api.Dtos.Enrolments;

/// <summary>Response body describing one Enrolment.</summary>
public class EnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; }
    public DateTime EventDate { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; }
}
