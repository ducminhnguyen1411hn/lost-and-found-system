using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.FoundItems;

/// <summary>Public list + search/filter/pagination state (FR-FOUND-04). Bound from the query string;
/// the controller fills <see cref="Results"/> and the select lists.</summary>
public class FoundItemSearchViewModel
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string? Tag { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? FoundFrom { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? FoundTo { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();

    public PagedResult<FoundItemListItemViewModel> Results { get; set; } = new();
}
