using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nairabox.Application.Common.Models;
using Nairabox.Infrastructure.Data;

namespace Nairabox.Api.Controllers;

/// <summary>
/// Provides event category lookup for filtering and discovery.
/// </summary>
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly NairaboxDbContext _db;

    public CategoriesController(NairaboxDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all event categories sorted alphabetically.
    /// </summary>
    /// <returns>List of categories with name, slug, icon, and description</returns>
    /// <response code="200">Categories retrieved</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.EventCategories
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(categories.Select(c => new
        {
            c.Id,
            c.Name,
            c.Slug,
            c.Icon,
            c.Description
        })));
    }
}
