using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Results;

/// <summary>Request body for POST /api/results.</summary>
public class CreateResultRequest
{
    [Required]
    public int EnrolmentId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int FinishTimeSeconds { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int FinishPosition { get; set; }
}

/// <summary>Request body for PUT /api/results/{id}.</summary>
public class UpdateResultRequest
{
    [Required, Range(1, int.MaxValue)]
    public int FinishTimeSeconds { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int FinishPosition { get; set; }
}
