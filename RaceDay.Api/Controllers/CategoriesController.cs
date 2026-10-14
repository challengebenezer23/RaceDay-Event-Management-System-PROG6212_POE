using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Age/distance categories offered within a RaceDay event.</summary>
[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all categories available for a specific event.</summary>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = $"No event exists with ID {eventId}." });
        }

        var categories = await _context.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(ToResponse));
    }

    /// <summary>Creates a new category for an event owned by the authenticated Organiser.</summary>
    /// <response code="201">The category was created.</response>
    /// <response code="400">A field was invalid (e.g. MinAge greater than MaxAge).</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryRequest request)
    {
        if (request.MinAge > request.MaxAge)
        {
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });
        }

        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event exists with ID {eventId}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        }

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKM = request.DistanceKM
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(category));
    }

    /// <summary>Updates a category belonging to an event owned by the authenticated Organiser.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="400">A field was invalid.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No category exists with that ID.</response>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (request.MinAge > request.MaxAge)
        {
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });
        }

        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null)
        {
            return NotFound(new { message = $"No category exists with ID {id}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (category.Event.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own the event this category belongs to." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKM = request.DistanceKM;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(category));
    }

    /// <summary>Deletes a category, provided no Participant has already enrolled in it.</summary>
    /// <response code="200">The category was deleted.</response>
    /// <response code="401">No one is logged in.</response>
    /// <response code="403">The caller does not own the parent event.</response>
    /// <response code="404">No category exists with that ID.</response>
    /// <response code="409">The category has active enrolments and cannot be deleted.</response>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(RaceDayRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null)
        {
            return NotFound(new { message = $"No category exists with ID {id}." });
        }

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (category.Event.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own the event this category belongs to." });
        }

        var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.CategoryID == id);
        if (hasEnrolments)
        {
            return Conflict(new { message = "This category has active enrolments and cannot be deleted." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Category deleted." });
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        CategoryName = c.CategoryName,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        DistanceKM = c.DistanceKM
    };
}
