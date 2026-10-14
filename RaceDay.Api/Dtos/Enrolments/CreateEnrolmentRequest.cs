using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Enrolments;

/// <summary>Request body for POST /api/enrolments.</summary>
public class CreateEnrolmentRequest
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }
}
