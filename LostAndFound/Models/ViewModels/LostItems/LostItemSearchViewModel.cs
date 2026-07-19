using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LostAndFound.Models.ViewModels.LostItems;

/// <summary>Public lost-item list + search/filter/pagination state. Bound from the query string.</summary>
public class LostItemSearchViewModel : IValidatableObject
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string? Tag { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? LostFrom { get; set; }

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? LostTo { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();

    public PagedResult<LostItemListItemViewModel> Results { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LostFrom.HasValue && LostTo.HasValue && LostFrom.Value > LostTo.Value)
            yield return new ValidationResult(
                "\"Mất từ\" phải trước hoặc bằng \"Đến\".",
                new[] { nameof(LostFrom), nameof(LostTo) });
    }
}
