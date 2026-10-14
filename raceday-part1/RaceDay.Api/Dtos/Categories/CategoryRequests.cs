using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Categories;

/// <summary>Request body for POST /api/events/{eventId}/categories.</summary>
public class CreateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; }

    [Range(0, 120)]
    public int MinAge { get; set; } = 0;

    [Range(0, 120)]
    public int MaxAge { get; set; } = 99;

    [Required, Range(0.1, 9999.99)]
    public decimal DistanceKM { get; set; }
}

/// <summary>Request body for PUT /api/categories/{id}.</summary>
public class UpdateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; }

    [Range(0, 120)]
    public int MinAge { get; set; }

    [Range(0, 120)]
    public int MaxAge { get; set; }

    [Required, Range(0.1, 9999.99)]
    public decimal DistanceKM { get; set; }
}
