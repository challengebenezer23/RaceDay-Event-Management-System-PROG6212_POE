using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Events;

/// <summary>Request body for POST /api/events.</summary>
public class CreateEventRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string EventName { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; }

    [Required, Range(0.1, 9999.99)]
    public decimal Distance { get; set; }

    /// <summary>Must be one of "Run", "Walk" or "Cycle".</summary>
    [Required]
    public string EventType { get; set; }
}

/// <summary>Request body for PUT /api/events/{id}.</summary>
public class UpdateEventRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string EventName { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; }

    [Required, Range(0.1, 9999.99)]
    public decimal Distance { get; set; }

    [Required]
    public string EventType { get; set; }
}
