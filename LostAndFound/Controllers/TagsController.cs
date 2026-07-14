using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers;

/// <summary>Thin controller for tag autocomplete (FR-TAG-04). Public endpoint for tag suggestions.</summary>
public class TagsController : Controller
{
    private readonly ITagService _tags;

    public TagsController(ITagService tags)
    {
        _tags = tags;
    }

    // GET /Tags/Suggest?q=vien
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Suggest(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<string>());

        var suggestions = await _tags.SuggestTagsAsync(q);
        return Json(suggestions);
    }
}
