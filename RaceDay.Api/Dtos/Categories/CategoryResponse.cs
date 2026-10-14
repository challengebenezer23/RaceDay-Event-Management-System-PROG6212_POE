namespace RaceDay.Api.Dtos.Categories;

/// <summary>Response body describing one Category.</summary>
public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public decimal DistanceKM { get; set; }
}
