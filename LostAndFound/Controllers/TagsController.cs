using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers;

public class TagsController : Controller
{
    private readonly ITagService _tags;

    public TagsController(ITagService tags)
    {
        _tags = tags;
    }

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
